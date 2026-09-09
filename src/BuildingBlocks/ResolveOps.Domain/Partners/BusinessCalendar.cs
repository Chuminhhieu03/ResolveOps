namespace ResolveOps.Domain.Partners;

/// <summary>
/// Business calendar aggregate root (spec §15.2).
///
/// A business calendar defines working days and hours for a tenant, used
/// to calculate SLA deadlines and claim submission deadlines correctly.
///
/// Invariants:
/// - Name is unique within a tenant (UNIQUE INDEX on tenant_id, name).
/// - WorkingDaysMask uses bit flags: bit 0 = Sunday, bit 1 = Monday, ..., bit 6 = Saturday.
/// - WorkingStart must be before WorkingEnd.
/// - A holiday date can appear at most once per calendar (enforced by DB unique index).
/// - Version is used for optimistic concurrency.
/// </summary>
public sealed class BusinessCalendar
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Human-readable calendar name. Unique within tenant.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// IANA timezone identifier for this calendar, e.g. "Asia/Ho_Chi_Minh".
    /// Used when calculating deadline times.
    /// </summary>
    public string Timezone { get; private set; } = "UTC";

    /// <summary>
    /// Bitmask of working days.
    /// Bit 0 = Sunday, bit 1 = Monday, ..., bit 6 = Saturday.
    /// E.g. Mon–Fri = 0b0111110 = 62.
    /// (Assumption A-Phase3-002: bitmask chosen over child table for MVP simplicity.)
    /// </summary>
    public int WorkingDaysMask { get; private set; }

    /// <summary>Start of the working day (local time in the calendar's timezone).</summary>
    public TimeOnly WorkingStart { get; private set; }

    /// <summary>End of the working day (local time in the calendar's timezone).</summary>
    public TimeOnly WorkingEnd { get; private set; }

    /// <summary>Active or Archived.</summary>
    public string Status { get; private set; } = BusinessCalendarStatus.Active;

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (spec §15.1).</summary>
    public long Version { get; private set; }

    private readonly List<BusinessCalendarHoliday> _holidays = [];

    /// <summary>Holiday overrides for this calendar.</summary>
    public IReadOnlyList<BusinessCalendarHoliday> Holidays => _holidays.AsReadOnly();

    // EF Core requires a parameterless constructor.
    private BusinessCalendar() { }

    public static BusinessCalendar Create(
        Guid tenantId,
        string name,
        string timezone,
        int workingDaysMask,
        TimeOnly workingStart,
        TimeOnly workingEnd,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new BusinessCalendar
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Name = name.Trim(),
            Timezone = timezone,
            WorkingDaysMask = workingDaysMask,
            WorkingStart = workingStart,
            WorkingEnd = workingEnd,
            Status = BusinessCalendarStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void Update(
        string name,
        string timezone,
        int workingDaysMask,
        TimeOnly workingStart,
        TimeOnly workingEnd,
        TimeProvider timeProvider)
    {
        Name = name.Trim();
        Timezone = timezone;
        WorkingDaysMask = workingDaysMask;
        WorkingStart = workingStart;
        WorkingEnd = workingEnd;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }

    public void Archive(TimeProvider timeProvider)
    {
        Status = BusinessCalendarStatus.Archived;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }

    /// <summary>
    /// Adds a holiday to this calendar.
    /// If a holiday on the same date already exists this is a no-op — duplicate enforcement
    /// is done by the unique DB index; the handler should check before calling.
    /// </summary>
    public BusinessCalendarHoliday AddHoliday(
        DateOnly holidayDate,
        string name,
        bool isWorkingOverride,
        TimeProvider timeProvider)
    {
        var holiday = BusinessCalendarHoliday.Create(
            TenantId, Id, holidayDate, name, isWorkingOverride);
        _holidays.Add(holiday);
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
        return holiday;
    }

    /// <summary>
    /// Removes a holiday entry by its ID.
    /// Returns false when the holiday was not found in this calendar.
    /// </summary>
    public bool RemoveHoliday(Guid holidayId, TimeProvider timeProvider)
    {
        var holiday = _holidays.FirstOrDefault(h => h.Id == holidayId);
        if (holiday is null)
        {
            return false;
        }

        _holidays.Remove(holiday);
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
        return true;
    }

    /// <summary>Returns true if the given day-of-week bit is set in the working days mask.</summary>
    public bool IsWorkingDay(DayOfWeek dayOfWeek)
    {
        var bit = 1 << (int)dayOfWeek;
        return (WorkingDaysMask & bit) != 0;
    }
}

/// <summary>Valid values for <see cref="BusinessCalendar.Status"/>.</summary>
public static class BusinessCalendarStatus
{
    public const string Active = "Active";
    public const string Archived = "Archived";
}

/// <summary>
/// Helper constants for common working-day bitmasks.
/// Bit 0 = Sunday, bit 6 = Saturday.
/// </summary>
public static class WorkingDaysMasks
{
    /// <summary>Monday to Friday = bits 1–5 set = 0b0111110 = 62.</summary>
    public const int MondayToFriday = 0b0111110;

    /// <summary>Monday to Saturday = bits 1–6 set = 0b1111110 = 126.</summary>
    public const int MondayToSaturday = 0b1111110;

    /// <summary>All days = bits 0–6 set = 0b1111111 = 127.</summary>
    public const int AllDays = 0b1111111;
}
