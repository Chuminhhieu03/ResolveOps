using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.GetLocation;

public static class GetLocationEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/locations/{locationId:guid}", async (
            Guid locationId,
            GetLocationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(locationId, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: _ => Results.NotFound());
        })
        .WithName("GetLocation")
        .WithTags("Locations")
        .Produces<LocationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireViewLocations);
    }
}
