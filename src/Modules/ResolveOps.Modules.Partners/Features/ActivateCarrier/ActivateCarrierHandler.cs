using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.ActivateCarrier;

internal sealed class ActivateCarrierHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ActivateCarrierHandler(AppDbContext dbContext, TimeProvider timeProvider)
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

        carrier.Activate(_timeProvider);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
