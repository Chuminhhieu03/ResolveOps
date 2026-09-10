using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.DeactivateCarrier;

public sealed class DeactivateCarrierEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carriers/{carrierId:guid}/deactivate", async (
            Guid carrierId,
            DeactivateCarrierHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(carrierId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("DeactivateCarrier")
        .WithTags("Carriers")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCarriers);
    }
}
