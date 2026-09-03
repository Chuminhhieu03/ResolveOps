using ResolveOps.Domain;

namespace ResolveOps.Security;

/// <summary>
/// Represents the authenticated tenant context for the current request or job scope.
///
/// TenantId is always derived from the authenticated user's verified membership —
/// never from arbitrary request-body values (spec §10.6 rule 2).
/// </summary>
public interface ITenantContext
{
    /// <summary>Current tenant identifier resolved from the authenticated token.</summary>
    TenantId TenantId { get; }

    /// <summary>Current authenticated user identifier.</summary>
    UserId UserId { get; }

    /// <summary>Roles the user holds within the current tenant.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Returns true when the user holds the specified permission string.</summary>
    bool HasPermission(string permission);
}
