using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain.Identity;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.InviteUser;

public sealed class InviteUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public InviteUserHandler(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _context = context;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HandleAsync(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        // Check if user exists globally
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = ApplicationUser.Create(email, email, _timeProvider);
            // Default random password for invited users, they should do forgot password or we'd send an invite email
            var result = await _userManager.CreateAsync(user, Guid.NewGuid().ToString() + "aA1!");
            if (!result.Succeeded)
            {
                return false;
            }
        }

        // Check if already in tenant
        var existingMembership = await _context.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == _tenantContext.TenantId.Value && m.UserId == user.Id, cancellationToken);

        if (existingMembership != null)
        {
            // Already a member, ignore or return error? We'll just return true.
            return true;
        }

        // Create membership
        var membership = UserTenantMembership.Create(_tenantContext.TenantId.Value, user.Id, command.Roles, _timeProvider);
        _context.UserTenantMemberships.Add(membership);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
