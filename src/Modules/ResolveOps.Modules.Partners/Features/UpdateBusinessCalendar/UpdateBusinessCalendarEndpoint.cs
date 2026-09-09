using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;

public static class UpdateBusinessCalendarEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/business-calendars/{calendarId:guid}", async (
            Guid calendarId,
            UpdateBusinessCalendarRequest request,
            UpdateBusinessCalendarHandler handler,
            IValidator<UpdateBusinessCalendarCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateBusinessCalendarCommand(
                calendarId,
                request.Name,
                request.Timezone,
                request.WorkingDaysMask,
                request.WorkingStart,
                request.WorkingEnd,
                request.ExpectedVersion);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Code switch
                {
                    "CONCURRENCY_CONFLICT" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Concurrency Conflict",
                        detail: error.Message),
                    "VALIDATION_FAILED" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Validation Failed",
                        detail: error.Message),
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Update Business Calendar Failed",
                        detail: error.Message),
                });
        })
        .WithName("UpdateBusinessCalendar")
        .WithTags("BusinessCalendars")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageBusinessCalendars);
    }
}

public sealed record UpdateBusinessCalendarRequest(
    string Name,
    string Timezone,
    int WorkingDaysMask,
    TimeOnly WorkingStart,
    TimeOnly WorkingEnd,
    long ExpectedVersion);
