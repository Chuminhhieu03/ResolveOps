using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tracking.Features.ManualIngest;

public sealed class ManualIngestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tracking-events", async (
            ManualIngestCommand command,
            ManualIngestHandler handler,
            IValidator<ManualIngestCommand> validator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, correlationId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Accepted($"/api/tracking-events/receipts/{response.ReceiptId}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ManualIngestTrackingEvent")
        .WithTags("Tracking")
        .Produces<ManualIngestResponse>(StatusCodes.Status202Accepted)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
