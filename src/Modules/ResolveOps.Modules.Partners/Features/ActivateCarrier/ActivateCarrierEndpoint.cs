using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ActivateCarrier;

public static class ActivateCarrierEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carriers/{carrierId:guid}/activate", async (
            Guid carrierId,
            ActivateCarrierHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(carrierId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Code switch
                {
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Activate Carrier Failed",
                        detail: error.Message),
                });
        })
        .WithName("ActivateCarrier")
        .WithTags("Carriers")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCarriers);
    }
}
