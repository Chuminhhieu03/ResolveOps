using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.ListBusinessCalendars;

internal sealed class ListBusinessCalendarsHandler
{
    private readonly AppDbContext _dbContext;

    public ListBusinessCalendarsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListBusinessCalendarsResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var calendars = await _dbContext.BusinessCalendars
            .AsNoTracking()
            .OrderBy(bc => bc.Name)
            .Select(bc => new BusinessCalendarSummary(
                bc.Id,
                bc.Name,
                bc.Timezone,
                bc.Status))
            .ToListAsync(cancellationToken);

        return new ListBusinessCalendarsResponse(calendars, calendars.Count);
    }
}
