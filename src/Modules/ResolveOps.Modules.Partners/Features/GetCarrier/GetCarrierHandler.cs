using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.GetCarrier;

internal sealed class GetCarrierHandler
{
    private readonly AppDbContext _dbContext;

    public GetCarrierHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CarrierResponse>> HandleAsync(
        GetCarrierQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant query filter is applied automatically via EF Core global filter.
        var carrier = await _dbContext.Carriers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CarrierId, cancellationToken);

        if (carrier is null)
        {
            return DomainError.ResourceNotFound with { Message = "Carrier not found." };
        }

        return new CarrierResponse(
            carrier.Id,
            carrier.Code,
            carrier.Name,
            carrier.Status,
            carrier.ScacOrExternalCode,
            carrier.DefaultTimezone,
            carrier.ContactEmail,
            carrier.ClaimSubmissionChannel,
            carrier.CreatedAtUtc,
            carrier.UpdatedAtUtc,
            carrier.ConcurrencyStamp);
    }
}
