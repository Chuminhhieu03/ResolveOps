using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.GetBusinessCalendar;

public sealed class GetBusinessCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/business-calendars/{calendarId:guid}", async (
            Guid calendarId,
            GetBusinessCalendarHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetBusinessCalendarQuery(calendarId), cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetBusinessCalendar")
        .WithTags("BusinessCalendars")
        .Produces<BusinessCalendarResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        // Re-use location viewer role for business calendars by default, or tenant admin. 
        // We added a specific CanManageBusinessCalendars but no View permission in the plan.
        // We will just require RequireActiveTenantMembership and let anyone view the tenant's calendars.
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
