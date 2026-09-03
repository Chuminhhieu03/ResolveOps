using Microsoft.EntityFrameworkCore;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.DeactivateUser;

public sealed class DeactivateUserHandler
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DeactivateUserHandler(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<bool> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var membership = await _context.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == _tenantContext.TenantId.Value && m.UserId == userId, cancellationToken);

        if (membership == null)
        {
            return false;
        }

        membership.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
