namespace ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;

public sealed record UpdateBusinessCalendarCommand(
    Guid CalendarId,
    string Name,
    string Timezone,
    int WorkingDaysMask,
    TimeOnly WorkingStart,
    TimeOnly WorkingEnd,
    string ConcurrencyStamp);
