namespace ResolveOps.Modules.Partners.Features.RemoveHoliday;

public sealed record RemoveHolidayCommand(
    Guid CalendarId,
    Guid HolidayId);
