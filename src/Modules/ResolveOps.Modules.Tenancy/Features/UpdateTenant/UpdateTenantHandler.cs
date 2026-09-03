using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.UpdateTenant;

internal sealed class UpdateTenantHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public UpdateTenantHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId.Value, cancellationToken);

        if (tenant == null)
        {
            return DomainError.ResourceNotFound with { Message = "Tenant not found." };
        }

        // Apply EF's original value token for optimistic concurrency
        _dbContext.Entry(tenant).Property(t => t.Version).OriginalValue = command.ExpectedVersion;

        tenant.Update(command.Name, command.DefaultTimezone, command.DefaultCurrency, _timeProvider);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict with { Message = "The tenant was updated by another user. Please refresh and try again." };
        }
    }
}
