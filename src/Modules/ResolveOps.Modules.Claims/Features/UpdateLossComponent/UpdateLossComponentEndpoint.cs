using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ResolveOps.Modules.Claims.Features.UpdateLossComponent;

public static class UpdateLossComponentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/claims/{claimId}/loss-components/{componentId}", async (
            [FromRoute] Guid claimId,
            [FromRoute] Guid componentId,
            [FromBody] UpdateLossComponentRequest request,
            [FromServices] UpdateLossComponentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateLossComponentCommand(
                claimId,
                componentId,
                request.Description,
                request.Quantity,
                request.UnitAmount,
                request.Amount,
                request.Currency,
                request.SourceDocumentId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.Error.Code == "CLAIM_NOT_FOUND" || result.Error.Code == "RESOURCE_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { result.Error.Code, result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("UpdateLossComponent")
        .WithTags("Claims");
    }
}

public record UpdateLossComponentRequest(
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
