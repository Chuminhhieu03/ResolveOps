using Microsoft.AspNetCore.Authorization;

namespace ResolveOps.Security;

/// <summary>
/// Authorization requirement ensuring the current user has an active membership
/// in the target tenant.
/// </summary>
public sealed class TenantMembershipRequirement : IAuthorizationRequirement
{
}
