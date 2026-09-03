using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ResolveOps.Domain.Identity;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Features.Auth;

internal sealed class TokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public TokenGenerator(IConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    public string GenerateAccessToken(ApplicationUser user, UserTenantMembership membership)
    {
        var secretKey = _configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");
        var issuer = _configuration["Jwt:Issuer"] ?? "ResolveOps";
        var audience = _configuration["Jwt:Audience"] ?? "ResolveOps";
        var expirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(AuthorizationPolicies.ClaimTypes.TenantId, membership.TenantId.ToString()),
            new(AuthorizationPolicies.ClaimTypes.MembershipId, membership.Id.ToString())
        };

        var roles = membership.GetRoles();
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var permissions = Permissions.GetPermissionsForRoles(roles);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AuthorizationPolicies.ClaimTypes.Permission, permission));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials,
            NotBefore = now
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
