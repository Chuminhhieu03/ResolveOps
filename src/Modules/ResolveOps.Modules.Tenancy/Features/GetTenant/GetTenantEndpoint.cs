using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.GetTenant;

public sealed class GetTenantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants/current", async (
            GetTenantHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTenantQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails()
            );
        })
        .WithName("GetCurrentTenant")
        .WithTags("Tenancy")
        .Produces<GetTenantResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
