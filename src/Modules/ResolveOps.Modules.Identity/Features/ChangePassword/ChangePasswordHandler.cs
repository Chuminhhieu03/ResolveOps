using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Identity;
using ResolveOps.Modules.Identity.Features.Audit;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Features.ChangePassword;

internal sealed class ChangePasswordHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly AuditService _auditService;
    private readonly TimeProvider _timeProvider;

    public ChangePasswordHandler(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ITenantContext tenantContext,
        AuditService auditService,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(_tenantContext.UserId.Value.ToString());
        if (user == null)
        {
            return DomainError.ResourceNotFound with { Message = "User not found." };
        }

        var identityResult = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!identityResult.Succeeded)
        {
            return DomainError.ValidationFailed with { Message = string.Join("; ", identityResult.Errors.Select(e => e.Description)) };
        }

        // Revoke all refresh token families for this user across ALL tenants, forcing re-login everywhere.
        var sessions = await _dbContext.RefreshTokenSessions
            .IgnoreQueryFilters()
            .Where(s => s.UserId == user.Id && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(_timeProvider);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditService.LogPasswordChanged(user.Id);

        return Result.Success();
    }
}
