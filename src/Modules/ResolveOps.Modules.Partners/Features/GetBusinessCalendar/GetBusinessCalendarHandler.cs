using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.GetBusinessCalendar;

internal sealed class GetBusinessCalendarHandler
{
    private readonly AppDbContext _dbContext;

    public GetBusinessCalendarHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BusinessCalendarResponse>> HandleAsync(GetBusinessCalendarQuery query, CancellationToken cancellationToken)
    {
        var calendar = await _dbContext.BusinessCalendars
            .Include(bc => bc.Holidays)
            .AsNoTracking()
            .FirstOrDefaultAsync(bc => bc.Id == query.CalendarId, cancellationToken);

        if (calendar is null)
        {
            return DomainError.ResourceNotFound with { Message = "Business calendar not found." };
        }

        var holidayResponses = calendar.Holidays
            .OrderBy(h => h.HolidayDate)
            .Select(h => new BusinessCalendarHolidayResponse(
                h.Id,
                h.HolidayDate,
                h.Name,
                h.IsWorkingOverride))
            .ToList();

        return new BusinessCalendarResponse(
            calendar.Id,
            calendar.Name,
            calendar.Timezone,
            calendar.WorkingDaysMask,
            calendar.WorkingStart,
            calendar.WorkingEnd,
            calendar.Status,
            holidayResponses,
            calendar.CreatedAtUtc,
            calendar.UpdatedAtUtc,
            calendar.Version);
    }
}
