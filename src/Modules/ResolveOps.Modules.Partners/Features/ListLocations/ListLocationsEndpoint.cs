using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListLocations;

public static class ListLocationsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/locations", async (
            ListLocationsHandler handler,
            CancellationToken cancellationToken,
            string? countryCode = null,
            int page = 1,
            int pageSize = 50) =>
        {
            var result = await handler.HandleAsync(countryCode, page, pageSize, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "List Locations Failed",
                    detail: error.Message));
        })
        .WithName("ListLocations")
        .WithTags("Locations")
        .Produces<ListLocationsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewLocations);
    }
}
