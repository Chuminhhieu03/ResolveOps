using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.GetLocation;

internal sealed class GetLocationHandler
{
    private readonly AppDbContext _dbContext;

    public GetLocationHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LocationResponse>> HandleAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await _dbContext.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location is null)
        {
            return DomainError.ResourceNotFound with { Message = "Location not found." };
        }

        return new LocationResponse(
            location.Id,
            location.Code,
            location.Name,
            location.AddressLine1,
            location.AddressLine2,
            location.City,
            location.Region,
            location.PostalCode,
            location.CountryCode,
            location.Timezone,
            location.Latitude,
            location.Longitude,
            location.CreatedAtUtc,
            location.UpdatedAtUtc,
            location.ConcurrencyStamp);
    }
}
