using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.AddHoliday;

public sealed class AddHolidayEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/business-calendars/{calendarId:guid}/holidays", async (
            Guid calendarId,
            AddHolidayRequest request,
            AddHolidayHandler handler,
            IValidator<AddHolidayCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var command = new AddHolidayCommand(
                calendarId,
                request.HolidayDate,
                request.Name,
                request.IsWorkingOverride);

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
        .WithName("AddHoliday")
        .WithTags("BusinessCalendars")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageBusinessCalendars);
    }
}

public sealed record AddHolidayRequest(
    DateOnly HolidayDate,
    string Name,
    bool IsWorkingOverride);
