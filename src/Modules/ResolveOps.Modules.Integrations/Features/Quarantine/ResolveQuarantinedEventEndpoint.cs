using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

public sealed class ResolveQuarantinedEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/integration-operations/quarantined-events/{id:guid}/resolve", async (
            Guid id,
            ResolveQuarantinedEventRequest request,
            ResolveQuarantinedEventHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var correlationId = httpContext.TraceIdentifier;
            var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value;
            var userId = Guid.TryParse(subClaim, out var parsedUserId) ? parsedUserId : (Guid?)null;

            var result = await handler.HandleAsync(id, request, userId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ResolveQuarantinedEvent")
        .WithTags("Integrations")
        .Produces<QuarantinedEventSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
