using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.GetClaimById;

public sealed class GetClaimByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/claims/{claimId:guid}", async (
            [FromRoute] Guid claimId,
            [FromServices] GetClaimByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetClaimByIdQuery(claimId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CLAIM_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("GetClaimById")
        .WithTags("Claims");
    }
}
