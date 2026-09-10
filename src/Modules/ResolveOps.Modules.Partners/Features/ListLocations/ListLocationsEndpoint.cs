using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListLocations;

public sealed class ListLocationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
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
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListLocations")
        .WithTags("Locations")
        .Produces<ListLocationsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewLocations);
    }
}
