using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ResolveOps.Modules.Claims.Features.AddLossComponent;

public static class AddLossComponentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId}/loss-components", async (
            [FromRoute] Guid claimId,
            [FromBody] AddLossComponentRequest request,
            [FromServices] AddLossComponentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddLossComponentCommand(
                claimId,
                request.ComponentType,
                request.Description,
                request.Quantity,
                request.UnitAmount,
                request.Amount,
                request.Currency,
                request.SourceDocumentId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                success => Results.Created($"/api/claims/{claimId}/loss-components/{success}", new { Id = success }),
                error => error.Code == "CLAIM_NOT_FOUND"
                    ? Results.NotFound()
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("AddLossComponent")
        .WithTags("Claims");
    }
}

public record AddLossComponentRequest(
    string ComponentType,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
