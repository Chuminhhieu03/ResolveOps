using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ResolveOps.Domain;
using ResolveOps.Domain.Identity;
using ResolveOps.Modules.Identity.Features.Audit;
using ResolveOps.Modules.Identity.Features.Auth;
using ResolveOps.Modules.Identity.Features.Login;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Identity.Features.Refresh;

internal sealed class RefreshHandler
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly AuditService _auditService;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    public RefreshHandler(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ITokenGenerator tokenGenerator,
        AuditService auditService,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _auditService = auditService;
        _timeProvider = timeProvider;
        _configuration = configuration;
    }

    public async Task<Result<LoginResult>> HandleAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return DomainError.ValidationFailed with { Message = "Refresh token is missing." };
        }

        var hashedToken = _tokenGenerator.HashToken(command.Token);
        var ipHash = string.IsNullOrEmpty(command.IpAddress) ? null : HashString(command.IpAddress);
        var userAgentHash = string.IsNullOrEmpty(command.UserAgent) ? null : HashString(command.UserAgent);

        // Find the session ignoring query filters because tenant isn't known yet
        var session = await _dbContext.RefreshTokenSessions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TokenHash == hashedToken, cancellationToken);

        if (session == null)
        {
            // Token not found, or it was a reused token? 
            // In a more complex setup, we'd look up by FamilyId to detect reuse. 
            // For Phase 2, we just return unauthorized.
            return DomainError.Forbidden with { Message = "Invalid or expired refresh token." };
        }

        if (session.IsRevoked || session.ExpiresAtUtc < _timeProvider.GetUtcNow())
        {
            // Token reuse detected or expired
            _auditService.LogTokenReuseDetected(session.TenantId, session.UserId, session.FamilyId);
            session.Revoke(_timeProvider);

            // Should also revoke the whole family, but since we create a new row per refresh,
            // we'd need to revoke all sessions with the same FamilyId.
            var familySessions = await _dbContext.RefreshTokenSessions
                .IgnoreQueryFilters()
                .Where(s => s.FamilyId == session.FamilyId && !s.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var s in familySessions)
            {
                s.Revoke(_timeProvider);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return DomainError.Forbidden with { Message = "Invalid or expired refresh token." };
        }

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user == null)
        {
            return DomainError.Forbidden with { Message = "User not found." };
        }

        var membership = await _dbContext.UserTenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.TenantId == session.TenantId, cancellationToken);

        if (membership == null || !membership.IsActive)
        {
            return DomainError.Forbidden with { Message = "User does not have an active membership in the specified tenant." };
        }

        // Rotate token
        session.Revoke(_timeProvider);

        var newAccessToken = _tokenGenerator.GenerateAccessToken(user, membership);
        var newRefreshTokenStr = _tokenGenerator.GenerateRefreshToken();
        var newHashedRefreshToken = _tokenGenerator.HashToken(newRefreshTokenStr);

        var expiresMinutes = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationMinutes", 1440 * 7);
        var expiresAt = _timeProvider.GetUtcNow().AddMinutes(expiresMinutes);

        var newSession = RefreshTokenSession.CreateRotated(
            session,
            newHashedRefreshToken,
            expiresAt,
            _timeProvider,
            ipHash,
            userAgentHash);

        _dbContext.RefreshTokenSessions.Add(newSession);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditService.LogTokenRefresh(session.TenantId, user.Id, session.FamilyId);

        return new LoginResult(newAccessToken, newRefreshTokenStr, expiresAt);
    }

    private static string HashString(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
