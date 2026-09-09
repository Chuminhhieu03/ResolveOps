using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.RemoveHoliday;

public static class RemoveHolidayEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
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
                onFailure: error => error.Code switch
                {
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Remove Holiday Failed",
                        detail: error.Message),
                });
        })
        .WithName("RemoveHoliday")
        .WithTags("BusinessCalendars")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageBusinessCalendars);
    }
}
