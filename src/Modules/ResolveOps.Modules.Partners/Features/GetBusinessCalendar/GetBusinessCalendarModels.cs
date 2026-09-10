namespace ResolveOps.Modules.Partners.Features.GetBusinessCalendar;

public sealed record GetBusinessCalendarQuery(Guid CalendarId);

public sealed record BusinessCalendarResponse(
    Guid Id,
    string Name,
    string Timezone,
    int WorkingDaysMask,
    TimeOnly WorkingStart,
    TimeOnly WorkingEnd,
    string Status,
    IReadOnlyList<BusinessCalendarHolidayResponse> Holidays,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string ConcurrencyStamp);

public sealed record BusinessCalendarHolidayResponse(
    Guid Id,
    DateOnly HolidayDate,
    string Name,
    bool IsWorkingOverride);
