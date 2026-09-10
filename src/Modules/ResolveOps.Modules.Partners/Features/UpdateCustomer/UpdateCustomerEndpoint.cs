using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateCustomer;

public sealed class UpdateCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
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
    string ConcurrencyStamp);
