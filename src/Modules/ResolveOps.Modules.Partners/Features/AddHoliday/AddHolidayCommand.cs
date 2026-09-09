namespace ResolveOps.Modules.Partners.Features.AddHoliday;

public sealed record AddHolidayCommand(
    Guid CalendarId,
    DateOnly HolidayDate,
    string Name,
    bool IsWorkingOverride);
