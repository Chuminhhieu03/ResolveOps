using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateCarrier;

public sealed class UpdateCarrierEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/carriers/{carrierId:guid}", async (
            Guid carrierId,
            UpdateCarrierRequest request,
            UpdateCarrierHandler handler,
            IValidator<UpdateCarrierCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCarrierCommand(
                carrierId,
                request.Name,
                request.ScacOrExternalCode,
                request.DefaultTimezone,
                request.ContactEmail,
                request.ClaimSubmissionChannel,
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
        .WithName("UpdateCarrier")
        .WithTags("Carriers")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageCarriers);
    }
}

// Separate request body record so carrierId comes from the route, not the body.
public sealed record UpdateCarrierRequest(
    string Name,
    string? ScacOrExternalCode,
    string? DefaultTimezone,
    string? ContactEmail,
    string ClaimSubmissionChannel,
    string ConcurrencyStamp);
