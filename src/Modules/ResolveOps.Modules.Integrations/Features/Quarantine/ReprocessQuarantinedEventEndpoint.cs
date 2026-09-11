using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

public sealed class ReprocessQuarantinedEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/integration-operations/quarantined-events/{id:guid}/reprocess", async (
            Guid id,
            ReprocessQuarantinedEventHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(id, correlationId, cancellationToken);

            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ReprocessQuarantinedEvent")
        .WithTags("Integrations")
        .Produces<QuarantinedEventSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
