using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.DeactivateUser;

public sealed class DeactivateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tenancy/users/{userId:guid}/deactivate", async (
            Guid userId,
            [FromServices] DeactivateUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(userId, cancellationToken);

            if (!result)
            {
                return Results.NotFound("User not found in tenant.");
            }

            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .RequireAuthorization(policy => policy.RequireClaim(Permissions.CanManageUsers))
        .WithName("DeactivateUser")
        .WithTags("Tenancy");
    }
}
