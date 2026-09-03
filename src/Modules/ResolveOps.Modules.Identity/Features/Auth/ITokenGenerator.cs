using ResolveOps.Domain.Identity;

namespace ResolveOps.Modules.Identity.Features.Auth;

/// <summary>
/// Generates JWT access tokens and opaque refresh tokens.
/// </summary>
public interface ITokenGenerator
{
    string GenerateAccessToken(ApplicationUser user, UserTenantMembership membership);
    string GenerateRefreshToken();
    string HashToken(string token);
}
