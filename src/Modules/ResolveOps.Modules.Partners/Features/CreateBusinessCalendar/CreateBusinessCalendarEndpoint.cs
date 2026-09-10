using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateBusinessCalendar;

public sealed class CreateBusinessCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/business-calendars", async (
            CreateBusinessCalendarCommand command,
            CreateBusinessCalendarHandler handler,
            IValidator<CreateBusinessCalendarCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: id => Results.Created($"/api/business-calendars/{id}", new { CalendarId = id }),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateBusinessCalendar")
        .WithTags("BusinessCalendars")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireManageBusinessCalendars);
    }
}
