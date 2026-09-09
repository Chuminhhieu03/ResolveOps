using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.RemoveHoliday;

internal sealed class RemoveHolidayHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RemoveHolidayHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(RemoveHolidayCommand command, CancellationToken cancellationToken)
    {
        var calendar = await _dbContext.BusinessCalendars
            .Include(bc => bc.Holidays)
            .FirstOrDefaultAsync(bc => bc.Id == command.CalendarId, cancellationToken);

        if (calendar is null)
        {
            return DomainError.ResourceNotFound with { Message = "Business calendar not found." };
        }

        calendar.RemoveHoliday(command.HolidayId, _timeProvider);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
