using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Tenancy.Features.ListTenants;

internal sealed class ListTenantsHandler
{
    private readonly AppDbContext _dbContext;

    public ListTenantsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListTenantsResponse>> HandleAsync(ListTenantsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(
                t.Id,
                t.Code,
                t.Name,
                t.Status,
                t.DefaultTimezone,
                t.DefaultCurrency))
            .ToListAsync(cancellationToken);

        return new ListTenantsResponse(tenants);
    }
}
