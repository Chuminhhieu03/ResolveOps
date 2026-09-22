using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.RemoveLossComponent;

public sealed class RemoveLossComponentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/claims/{claimId:guid}/loss-components/{componentId:guid}", async (
            [FromRoute] Guid claimId,
            [FromRoute] Guid componentId,
            [FromServices] RemoveLossComponentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveLossComponentCommand(claimId, componentId);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.Error.Code == "CLAIM_NOT_FOUND" || result.Error.Code == "RESOURCE_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { result.Error.Code, result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("RemoveLossComponent")
        .WithTags("Claims");
    }
}
