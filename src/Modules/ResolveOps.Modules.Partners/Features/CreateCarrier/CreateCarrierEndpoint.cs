using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateCarrier;

public static class CreateCarrierEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carriers", async (
            CreateCarrierCommand command,
            CreateCarrierHandler handler,
            IValidator<CreateCarrierCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/carriers/{response.CarrierId}", response),
                onFailure: error => error.Code switch
                {
                    "VALIDATION_FAILED" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Duplicate Carrier Code",
                        detail: error.Message),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Create Carrier Failed",
                        detail: error.Message),
                });
        })
        .WithName("CreateCarrier")
        .WithTags("Carriers")
        .Produces<CreateCarrierResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCarriers);
    }
}
