using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListCarriers;

public sealed class ListCarriersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carriers", async (
            ListCarriersHandler handler,
            CancellationToken cancellationToken,
            string? status = null,
            int page = 1,
            int pageSize = 50) =>
        {
            var query = new ListCarriersQuery(status, page, pageSize);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListCarriers")
        .WithTags("Carriers")
        .Produces<ListCarriersResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCarriers);
    }
}
