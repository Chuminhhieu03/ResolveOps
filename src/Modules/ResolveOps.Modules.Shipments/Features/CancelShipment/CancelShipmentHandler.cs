using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Shipments.Features.CancelShipment;

public sealed record CancelShipmentCommand(Guid ShipmentId, string ConcurrencyStamp);

internal sealed class CancelShipmentHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CancelShipmentHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        CancelShipmentCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant query filter applied automatically.
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == command.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            return DomainError.ResourceNotFound;
        }

        shipment.ConcurrencyStamp = command.ConcurrencyStamp;

        var cancelResult = shipment.Cancel(_timeProvider);
        if (!cancelResult.IsSuccess)
        {
            return cancelResult;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.Failure(
                "ERR_CONCURRENCY_CONFLICT",
                "The shipment was modified by another operation. Refresh and try again.");
        }
        catch (UniqueConstraintException)
        {
            // Not expected on cancel but guard defensively.
            return DomainError.Failure("ERR_SHIPMENT_CONFLICT", "A conflict occurred while cancelling the shipment.");
        }

        return Result.Success();
    }
}
