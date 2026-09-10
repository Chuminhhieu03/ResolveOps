using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ActivateCarrier;

public sealed class ActivateCarrierEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carriers/{carrierId:guid}/activate", async (
            Guid carrierId,
            ActivateCarrierHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(carrierId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ActivateCarrier")
        .WithTags("Carriers")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCarriers);
    }
}
