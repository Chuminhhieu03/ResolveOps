using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.GetCarrier;

public static class GetCarrierEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carriers/{carrierId:guid}", async (
            Guid carrierId,
            GetCarrierHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetCarrierQuery(carrierId), cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: _ => Results.NotFound());
        })
        .WithName("GetCarrier")
        .WithTags("Carriers")
        .Produces<CarrierResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCarriers);
    }
}
