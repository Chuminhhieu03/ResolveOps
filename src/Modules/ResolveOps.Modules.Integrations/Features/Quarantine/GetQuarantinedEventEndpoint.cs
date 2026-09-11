using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

public sealed class GetQuarantinedEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/integration-operations/quarantined-events/{id:guid}", async (
            Guid id,
            GetQuarantinedEventHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);

            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetQuarantinedEvent")
        .WithTags("Integrations")
        .Produces<QuarantinedEventDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}
