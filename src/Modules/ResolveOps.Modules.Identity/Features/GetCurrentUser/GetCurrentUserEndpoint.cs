using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Features.GetCurrentUser;

public sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var handlerDelegate = async (
            GetCurrentUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails()
            );
        };

        app.MapGet("/api/identity/me", handlerDelegate)
            .WithName("GetCurrentUser")
            .WithTags("Identity")
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);

        app.MapGet("/api/users/me", handlerDelegate)
            .WithName("GetUsersMe")
            .WithTags("Identity")
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
