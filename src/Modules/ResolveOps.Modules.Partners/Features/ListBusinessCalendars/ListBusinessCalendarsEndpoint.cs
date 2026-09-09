using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListBusinessCalendars;

public static class ListBusinessCalendarsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/business-calendars", async (
            ListBusinessCalendarsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "List Business Calendars Failed",
                    detail: error.Message));
        })
        .WithName("ListBusinessCalendars")
        .WithTags("BusinessCalendars")
        .Produces<ListBusinessCalendarsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
