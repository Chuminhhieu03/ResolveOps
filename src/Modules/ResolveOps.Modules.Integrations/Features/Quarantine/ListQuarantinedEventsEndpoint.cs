using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

public sealed class ListQuarantinedEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/integration-operations/quarantined-events", async (
            ListQuarantinedEventsHandler handler,
            string? status,
            int? pageNumber,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var page = Math.Max(pageNumber ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 20, 1, 100);

            var result = await handler.HandleAsync(status, page, size, cancellationToken);

            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListQuarantinedEvents")
        .WithTags("Integrations")
        .Produces<ListQuarantinedEventsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
