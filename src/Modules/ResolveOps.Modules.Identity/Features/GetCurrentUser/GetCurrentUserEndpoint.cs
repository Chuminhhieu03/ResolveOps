using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Features.GetCurrentUser;

public static class GetCurrentUserEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/me", async (
            GetCurrentUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Failed to get user profile",
                    detail: error.Message)
            );
        })
        .WithName("GetCurrentUser")
        .WithTags("Identity")
        .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
