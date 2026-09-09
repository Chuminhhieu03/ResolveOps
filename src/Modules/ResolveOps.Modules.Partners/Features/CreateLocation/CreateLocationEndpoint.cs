using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateLocation;

public static class CreateLocationEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/locations", async (
            CreateLocationCommand command,
            CreateLocationHandler handler,
            IValidator<CreateLocationCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: id => Results.Created($"/api/locations/{id}", new { LocationId = id }),
                onFailure: error => error.Code switch
                {
                    "VALIDATION_FAILED" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Duplicate Location Code",
                        detail: error.Message),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Create Location Failed",
                        detail: error.Message),
                });
        })
        .WithName("CreateLocation")
        .WithTags("Locations")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireManageLocations);
    }
}
