using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ResolveOps.Domain;
using ResolveOps.Domain.Identity;
using ResolveOps.Modules.Identity.Features.Audit;
using ResolveOps.Modules.Identity.Features.Auth;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Identity.Features.Login;

public sealed record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

internal sealed class LoginHandler
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly AuditService _auditService;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    public LoginHandler(
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

    public async Task<Result<LoginResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null || user.IsSystemAccount)
        {
            _auditService.LogLoginFailure(command.Email, "UserNotFoundOrSystem", command.IpAddress);
            return DomainError.ValidationFailed with { Message = "Invalid email or password." };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!isPasswordValid)
        {
            _auditService.LogLoginFailure(command.Email, "InvalidPassword", command.IpAddress);
            return DomainError.ValidationFailed with { Message = "Invalid email or password." };
        }

        // Verify tenant membership
        var membership = await _dbContext.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.TenantId == command.TenantId, cancellationToken);

        if (membership == null || !membership.IsActive)
        {
            _auditService.LogLoginFailure(command.Email, "InactiveOrMissingMembership", command.IpAddress);
            return DomainError.Forbidden with { Message = "User does not have an active membership in the specified tenant." };
        }

        // Generate tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user, membership);
        var refreshTokenStr = _tokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _tokenGenerator.HashToken(refreshTokenStr);

        var expiresMinutes = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationMinutes", 1440 * 7); // 7 days
        var expiresAt = _timeProvider.GetUtcNow().AddMinutes(expiresMinutes);

        // Hash IP and UserAgent for privacy (spec §15.3)
        var ipHash = string.IsNullOrEmpty(command.IpAddress) ? null : HashString(command.IpAddress);
        var userAgentHash = string.IsNullOrEmpty(command.UserAgent) ? null : HashString(command.UserAgent);

        // Store refresh token session
        var session = RefreshTokenSession.CreateNew(
            command.TenantId,
            user.Id,
            hashedRefreshToken,
            expiresAt,
            _timeProvider,
            ipHash,
            userAgentHash);

        _dbContext.RefreshTokenSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditService.LogLoginSuccess(command.TenantId, user.Id, user.Email!, command.IpAddress);

        return new LoginResult(accessToken, refreshTokenStr, expiresAt);
    }

    private static string HashString(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
