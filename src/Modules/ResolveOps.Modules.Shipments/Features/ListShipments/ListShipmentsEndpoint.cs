using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Shipments.Features.ListShipments;

public sealed class ListShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shipments", async (
            int page,
            int pageSize,
            string? status,
            ListShipmentsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListShipmentsQuery(page, pageSize, status);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListShipments")
        .WithTags("Shipments")
        .Produces<ListShipmentsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewShipments);
    }
}
