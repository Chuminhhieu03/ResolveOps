using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;

public sealed class UpdateBusinessCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
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
                request.ConcurrencyStamp);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
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
    string ConcurrencyStamp);
