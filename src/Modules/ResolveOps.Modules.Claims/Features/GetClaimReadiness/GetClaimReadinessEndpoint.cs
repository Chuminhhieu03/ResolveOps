using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.GetClaimReadiness;

public sealed class GetClaimReadinessEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/claims/{claimId:guid}/readiness", async (
            [FromRoute] Guid claimId,
            [FromServices] GetClaimReadinessHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetClaimReadinessQuery(claimId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CLAIM_NOT_FOUND" || error.Code == "CASE_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("GetClaimReadiness")
        .WithTags("Claims");
    }
}
