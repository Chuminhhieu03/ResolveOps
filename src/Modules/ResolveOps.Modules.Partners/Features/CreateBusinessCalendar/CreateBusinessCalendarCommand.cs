namespace ResolveOps.Modules.Partners.Features.CreateBusinessCalendar;

public sealed record CreateBusinessCalendarCommand(
    string Name,
    string Timezone,
    int WorkingDaysMask,
    TimeOnly WorkingStart,
    TimeOnly WorkingEnd);
