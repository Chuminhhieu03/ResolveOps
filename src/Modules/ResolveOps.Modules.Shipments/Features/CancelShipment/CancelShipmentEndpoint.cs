using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Shipments.Features.CancelShipment;

public sealed class CancelShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shipments/{shipmentId:guid}/cancel", async (
            Guid shipmentId,
            CancelShipmentRequest request,
            CancelShipmentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CancelShipmentCommand(shipmentId, request.ConcurrencyStamp);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CancelShipment")
        .WithTags("Shipments")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(AuthorizationPolicies.RequireCancelShipment);
    }
}

public sealed record CancelShipmentRequest(string ConcurrencyStamp);
