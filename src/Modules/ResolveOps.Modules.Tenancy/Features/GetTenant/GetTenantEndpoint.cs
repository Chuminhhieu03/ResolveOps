using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.GetTenant;

public static class GetTenantEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants/current", async (
            GetTenantHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTenantQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Tenant Not Found",
                    detail: error.Message)
            );
        })
        .WithName("GetCurrentTenant")
        .WithTags("Tenancy")
        .Produces<GetTenantResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
