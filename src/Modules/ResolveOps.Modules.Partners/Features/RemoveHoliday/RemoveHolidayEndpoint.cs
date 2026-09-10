using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.RemoveHoliday;

public sealed class RemoveHolidayEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/business-calendars/{calendarId:guid}/holidays/{holidayId:guid}", async (
            Guid calendarId,
            Guid holidayId,
            RemoveHolidayHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveHolidayCommand(calendarId, holidayId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("RemoveHoliday")
        .WithTags("BusinessCalendars")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageBusinessCalendars);
    }
}
