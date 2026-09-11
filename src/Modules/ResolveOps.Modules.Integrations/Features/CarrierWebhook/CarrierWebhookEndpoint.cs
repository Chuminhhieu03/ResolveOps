using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Integrations.Features.CarrierWebhook;

public sealed class CarrierWebhookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/integrations/carriers/{carrierCode}/webhooks/tracking", async (
            string carrierCode,
            CarrierWebhookHandler handler,
            HttpRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Read raw body bytes for HMAC signature verification
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream, cancellationToken);
            var bodyBytes = memoryStream.ToArray();

            var signature = request.Headers["X-Signature"].FirstOrDefault();
            var tenantIdHeader = request.Headers["X-Tenant-Id"].FirstOrDefault();
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(
                carrierCode,
                bodyBytes,
                signature,
                tenantIdHeader,
                correlationId,
                cancellationToken);

            return result.Match(
                onSuccess: response => Results.Accepted(
                    $"/api/integrations/carriers/{carrierCode}/webhooks/tracking/receipts/{response.ReceiptId}",
                    response),
                onFailure: error => error.Code == "ERR_INVALID_SIGNATURE"
                    ? Results.Unauthorized()
                    : error.ToProblemDetails());
        })
        .WithName("IngestCarrierWebhook")
        .WithTags("Integrations")
        .AllowAnonymous()
        .Produces<CarrierWebhookResponse>(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
