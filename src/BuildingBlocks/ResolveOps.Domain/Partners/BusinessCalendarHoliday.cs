namespace ResolveOps.Domain.Partners;

/// <summary>
/// Business calendar holiday — child entity of <see cref="BusinessCalendar"/>.
///
/// A holiday entry marks a specific date as non-working (or as a working override).
/// Unique per (tenant_id, calendar_id, holiday_date) — spec §15.2.
/// </summary>
public sealed class BusinessCalendarHoliday
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CalendarId { get; private set; }

    /// <summary>The date of the holiday (date only, no time component).</summary>
    public DateOnly HolidayDate { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// When true, this entry marks the date as a working day despite normally being off.
    /// When false (the default), it marks the date as a non-working holiday.
    /// </summary>
    public bool IsWorkingOverride { get; private set; }

    // EF Core requires a parameterless constructor.
    private BusinessCalendarHoliday() { }

    internal static BusinessCalendarHoliday Create(
        Guid tenantId,
        Guid calendarId,
        DateOnly holidayDate,
        string name,
        bool isWorkingOverride)
    {
        return new BusinessCalendarHoliday
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CalendarId = calendarId,
            HolidayDate = holidayDate,
            Name = name.Trim(),
            IsWorkingOverride = isWorkingOverride,
        };
    }

    internal void UpdateName(string name)
    {
        Name = name.Trim();
    }
}
