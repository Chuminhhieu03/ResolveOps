using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.UpdateCarrier;

internal sealed class UpdateCarrierHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UpdateCarrierHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(UpdateCarrierCommand command, CancellationToken cancellationToken)
    {
        var carrier = await _dbContext.Carriers
            .FirstOrDefaultAsync(c => c.Id == command.CarrierId, cancellationToken);

        if (carrier is null)
        {
            return DomainError.ResourceNotFound with { Message = "Carrier not found." };
        }

        // Apply EF's original value for optimistic concurrency
        _dbContext.Entry(carrier).Property(c => c.ConcurrencyStamp).OriginalValue = command.ConcurrencyStamp;

        carrier.Update(
            command.Name,
            command.ScacOrExternalCode,
            command.DefaultTimezone,
            command.ContactEmail,
            command.ClaimSubmissionChannel,
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
                Message = "The carrier was updated by another user. Please refresh and try again."
            };
        }
    }
}
