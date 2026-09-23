using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Tenancy.Features.ListTenants;

public sealed class ListTenantsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants", async (
            ListTenantsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListTenantsQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails()
            );
        })
        .WithName("ListTenants")
        .WithTags("Tenancy")
        .Produces<ListTenantsResponse>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }
}
