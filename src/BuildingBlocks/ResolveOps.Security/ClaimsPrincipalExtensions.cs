using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ResolveOps.Security;

/// <summary>
/// Centralized extension methods for resolving authenticated User and Tenant identifiers
/// consistently across all minimal API endpoints and handlers.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's Guid from NameIdentifier or sub claim.
    /// Throws <see cref="UnauthorizedAccessException"/> if missing or invalid.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("User identifier claim is missing or invalid.");
        }

        return userId;
    }

    /// <summary>
    /// Attempts to extract the authenticated user's Guid, returning null if absent or unparseable.
    /// </summary>
    public static Guid? TryGetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null) return null;

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extracts the authenticated tenant's Guid from tenant_id claim.
    /// Throws <see cref="UnauthorizedAccessException"/> if missing or invalid.
    /// </summary>
    public static Guid GetTenantId(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var value = principal.FindFirstValue(AuthorizationPolicies.ClaimTypes.TenantId)
            ?? principal.FindFirstValue("tenant_id");

        if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant identifier claim is missing or invalid.");
        }

        return tenantId;
    }

    /// <summary>
    /// Attempts to extract the authenticated tenant's Guid, returning null if absent or unparseable.
    /// </summary>
    public static Guid? TryGetTenantId(this ClaimsPrincipal? principal)
    {
        if (principal == null) return null;

        var value = principal.FindFirstValue(AuthorizationPolicies.ClaimTypes.TenantId)
            ?? principal.FindFirstValue("tenant_id");

        return Guid.TryParse(value, out var tenantId) ? tenantId : null;
    }
}
