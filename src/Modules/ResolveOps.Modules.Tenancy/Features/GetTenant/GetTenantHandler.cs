using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.GetTenant;

internal sealed class GetTenantHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetTenantHandler(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetTenantResponse>> HandleAsync(GetTenantQuery query, CancellationToken cancellationToken)
    {
        // Tenant is the isolation root, so it doesn't have a global query filter applied to it directly
        // We explicitly query for the tenant ID from the ITenantContext
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId.Value, cancellationToken);

        if (tenant == null)
        {
            return DomainError.ResourceNotFound with { Message = "Tenant not found." };
        }

        return new GetTenantResponse(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.Status,
            tenant.DefaultTimezone,
            tenant.DefaultCurrency,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc,
            tenant.Version);
    }
}
