using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;

internal sealed class UpdateBusinessCalendarHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UpdateBusinessCalendarHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(UpdateBusinessCalendarCommand command, CancellationToken cancellationToken)
    {
        var calendar = await _dbContext.BusinessCalendars
            .FirstOrDefaultAsync(bc => bc.Id == command.CalendarId, cancellationToken);

        if (calendar is null)
        {
            return DomainError.ResourceNotFound with { Message = "Business calendar not found." };
        }

        // Check if updating name to an existing one (unique name per tenant)
        if (calendar.Name != command.Name.Trim())
        {
            var nameExists = await _dbContext.BusinessCalendars
                .AnyAsync(bc => bc.TenantId == calendar.TenantId && bc.Name == command.Name.Trim() && bc.Id != calendar.Id,
                    cancellationToken);

            if (nameExists)
            {
                return DomainError.ValidationFailed with
                {
                    Message = $"A business calendar with name '{command.Name}' already exists in this tenant."
                };
            }
        }

        // Assign client's concurrency stamp; AppDbContext handles original value and new stamp generation
        calendar.ConcurrencyStamp = command.ConcurrencyStamp;

        calendar.Update(
            command.Name,
            command.Timezone,
            command.WorkingDaysMask,
            command.WorkingStart,
            command.WorkingEnd,
            _timeProvider);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict with
            {
                Message = "The business calendar was updated by another user. Please refresh and try again."
            };
        }
    }
}
