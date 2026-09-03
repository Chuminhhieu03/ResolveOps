using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ResolveOps.Domain;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Infrastructure;

/// <summary>
/// Implements <see cref="ITenantContext"/> by reading claims from the current
/// authenticated HTTP request. If there's no authenticated user, or the claims
/// are missing, it throws (because endpoints requiring tenant context must be
/// secured with authorization policies).
/// </summary>
internal sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Lazy<TenantContextState> _state;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _state = new Lazy<TenantContextState>(InitializeState);
    }

    public TenantId TenantId => _state.Value.TenantId;
    public UserId UserId => _state.Value.UserId;
    public IReadOnlyList<string> Roles => _state.Value.Roles;

    public bool HasPermission(string permission)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null) return false;

        return principal.HasClaim(AuthorizationPolicies.ClaimTypes.Permission, permission);
    }

    private TenantContextState InitializeState()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Cannot resolve ITenantContext: request is not authenticated.");
        }

        var tenantIdClaim = principal.FindFirst(AuthorizationPolicies.ClaimTypes.TenantId)?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantGuid))
        {
            throw new InvalidOperationException("Cannot resolve ITenantContext: tenant_id claim is missing or invalid.");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userGuid))
        {
            throw new InvalidOperationException("Cannot resolve ITenantContext: nameidentifier claim is missing or invalid.");
        }

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return new TenantContextState(
            new TenantId(tenantGuid),
            new UserId(userGuid),
            roles);
    }

    private sealed record TenantContextState(TenantId TenantId, UserId UserId, IReadOnlyList<string> Roles);
}
