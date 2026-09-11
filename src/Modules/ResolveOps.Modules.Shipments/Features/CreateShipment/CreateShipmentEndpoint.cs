using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Shipments.Features.CreateShipment;

public sealed class CreateShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shipments", async (
            CreateShipmentCommand command,
            CreateShipmentHandler handler,
            IValidator<CreateShipmentCommand> validator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Read Idempotency-Key header (optional per spec §16.5)
            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

            // CorrelationId from middleware
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, idempotencyKey, correlationId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/shipments/{response.ShipmentId}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateShipment")
        .WithTags("Shipments")
        .Produces<CreateShipmentResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireCreateShipment);
    }
}
