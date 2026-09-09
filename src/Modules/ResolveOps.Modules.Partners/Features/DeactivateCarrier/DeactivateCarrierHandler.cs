using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.DeactivateCarrier;

internal sealed class DeactivateCarrierHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DeactivateCarrierHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(Guid carrierId, CancellationToken cancellationToken)
    {
        var carrier = await _dbContext.Carriers
            .FirstOrDefaultAsync(c => c.Id == carrierId, cancellationToken);

        if (carrier is null)
        {
            return DomainError.ResourceNotFound with { Message = "Carrier not found." };
        }

        carrier.Deactivate(_timeProvider);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
