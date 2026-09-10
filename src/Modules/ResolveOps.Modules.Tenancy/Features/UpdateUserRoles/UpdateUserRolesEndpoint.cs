using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.UpdateUserRoles;

public sealed class UpdateUserRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/tenancy/users/{userId:guid}/roles", async (
            Guid userId,
            [FromBody] UpdateUserRolesCommand command,
            [FromServices] UpdateUserRolesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { UserId = userId };
            var result = await handler.HandleAsync(cmd, cancellationToken);
            
            if (!result)
            {
                return Results.NotFound("User not found in tenant.");
            }

            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .RequireAuthorization(policy => policy.RequireClaim(Permissions.CanManageUsers))
        .WithName("UpdateUserRoles")
        .WithTags("Tenancy");
    }
}
