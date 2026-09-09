using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateCustomer;

public static class UpdateCustomerEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/customers/{customerId:guid}", async (
            Guid customerId,
            UpdateCustomerRequest request,
            UpdateCustomerHandler handler,
            IValidator<UpdateCustomerCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCustomerCommand(
                customerId,
                request.Name,
                request.Priority,
                request.DefaultTimezone,
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
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Update Customer Failed",
                        detail: error.Message),
                });
        })
        .WithName("UpdateCustomer")
        .WithTags("Customers")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCustomers);
    }
}

public sealed record UpdateCustomerRequest(
    string Name,
    string Priority,
    string? DefaultTimezone,
    long ExpectedVersion);
