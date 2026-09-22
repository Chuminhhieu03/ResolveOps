using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.CalculateClaimEligibility;

public sealed class CalculateClaimEligibilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/calculate-eligibility", async (
            [FromRoute] Guid claimId,
            [FromServices] CalculateClaimEligibilityHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CalculateClaimEligibilityCommand(claimId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CLAIM_NOT_FOUND" || error.Code == "CASE_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("CalculateClaimEligibility")
        .WithTags("Claims");
    }
}
