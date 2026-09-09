using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateCustomer;

public static class CreateCustomerEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers", async (
            CreateCustomerCommand command,
            CreateCustomerHandler handler,
            IValidator<CreateCustomerCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: id => Results.Created($"/api/customers/{id}", new { CustomerId = id }),
                onFailure: error => error.Code switch
                {
                    "VALIDATION_FAILED" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Duplicate Customer Code",
                        detail: error.Message),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Create Customer Failed",
                        detail: error.Message),
                });
        })
        .WithName("CreateCustomer")
        .WithTags("Customers")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCustomers);
    }
}
