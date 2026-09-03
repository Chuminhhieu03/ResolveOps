using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.ListTenantUsers;

public static class ListTenantUsersEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tenancy/users", async (
            [FromServices] ListTenantUsersHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .RequireAuthorization(policy => policy.RequireClaim(Permissions.CanManageUsers))
        .WithName("ListTenantUsers")
        .WithTags("Tenancy");
    }
}
