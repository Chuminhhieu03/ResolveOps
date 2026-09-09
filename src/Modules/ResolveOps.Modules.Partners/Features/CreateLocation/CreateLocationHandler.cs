using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Partners;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateLocation;

internal sealed class CreateLocationHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreateLocationHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var normalizedCode = command.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Locations
            .AsNoTracking()
            .AnyAsync(l => l.TenantId == tenantId && l.Code == normalizedCode,
                cancellationToken);

        if (codeExists)
        {
            return DomainError.ValidationFailed with
            {
                Message = $"A location with code '{command.Code}' already exists in this tenant."
            };
        }

        var location = Location.Create(
            tenantId,
            command.Code,
            command.Name,
            command.AddressLine1,
            command.AddressLine2,
            command.City,
            command.Region,
            command.PostalCode,
            command.CountryCode,
            command.Timezone,
            command.Latitude,
            command.Longitude,
            _timeProvider);

        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return location.Id;
    }
}
