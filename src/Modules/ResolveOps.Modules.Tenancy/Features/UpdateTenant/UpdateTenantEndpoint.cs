using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.UpdateTenant;

public static class UpdateTenantEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/tenants/current", async (
            UpdateTenantCommand command,
            UpdateTenantHandler handler,
            IValidator<UpdateTenantCommand> validator,
            CancellationToken cancellationToken) =>
        {
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
                    "CONCURRENCY_CONFLICT" => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Concurrency Conflict", detail: error.Message),
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Update Failed", detail: error.Message)
                }
            );
        })
        .WithName("UpdateCurrentTenant")
        .WithTags("Tenancy")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
