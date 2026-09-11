using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.UpdateUserRoles;

public sealed class UpdateUserRolesHandler
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public UpdateUserRolesHandler(AppDbContext context, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _context = context;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HandleAsync(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        var membership = await _context.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == _tenantContext.TenantId.Value && m.UserId == command.UserId, cancellationToken);

        if (membership == null)
        {
            return false;
        }

        // Use the domain method to update roles
        membership.UpdateRoles(command.Roles);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
