using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ResolveOps.Security;

/// <summary>
/// Evaluates <see cref="TenantMembershipRequirement"/> by checking if the current
/// authenticated context has a valid TenantId claim. The actual database check
/// for membership status is performed at token issuance/refresh time, so this
/// handler is purely claim-based (stateless).
/// </summary>
public sealed class TenantMembershipHandler : AuthorizationHandler<TenantMembershipRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMembershipRequirement requirement)
    {
        // Require the TenantId claim to be present in the JWT.
        var tenantIdClaim = context.User.FindFirst(c => c.Type == AuthorizationPolicies.ClaimTypes.TenantId);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out _))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
