using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.AddHoliday;

internal sealed class AddHolidayHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AddHolidayHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(AddHolidayCommand command, CancellationToken cancellationToken)
    {
        var calendar = await _dbContext.BusinessCalendars
            .Include(bc => bc.Holidays)
            .FirstOrDefaultAsync(bc => bc.Id == command.CalendarId, cancellationToken);

        if (calendar is null)
        {
            return DomainError.ResourceNotFound with { Message = "Business calendar not found." };
        }

        calendar.AddHoliday(command.HolidayDate, command.Name, command.IsWorkingOverride, _timeProvider);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UIX_BusinessCalendarHolidays_TenantCalendarDate") == true)
        {
            return DomainError.ValidationFailed with { Message = "A holiday with this date already exists in the calendar." };
        }
    }
}
