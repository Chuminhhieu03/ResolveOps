using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateCarrier;

public static class UpdateCarrierEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
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
                        title: "Update Carrier Failed",
                        detail: error.Message),
                });
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
    long ExpectedVersion);
