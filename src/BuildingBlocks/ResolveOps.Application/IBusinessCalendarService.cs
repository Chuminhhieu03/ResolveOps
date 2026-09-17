namespace ResolveOps.Application;

/// <summary>
/// Service calculating working-hours-aware deadlines and durations based on tenant business calendars (spec §15.2, §18.2).
/// </summary>
public interface IBusinessCalendarService
{
    /// <summary>
    /// Calculates a target deadline in UTC by advancing working minutes through the specified or default tenant business calendar.
    /// </summary>
    Task<DateTimeOffset> CalculateDeadlineAsync(
        Guid tenantId,
        DateTimeOffset startAtUtc,
        int durationMinutes,
        Guid? calendarId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds duration to an existing deadline respecting business calendar working hours and holidays.
    /// Used to shift deadlines forward when SLA clocks are resumed after being paused.
    /// </summary>
    Task<DateTimeOffset> AddDurationAsync(
        Guid tenantId,
        DateTimeOffset currentDueAtUtc,
        TimeSpan durationToAdd,
        Guid? calendarId = null,
        CancellationToken cancellationToken = default);
}
