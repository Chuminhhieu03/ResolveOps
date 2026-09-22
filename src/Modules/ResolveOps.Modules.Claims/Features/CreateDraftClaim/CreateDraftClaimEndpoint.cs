using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public sealed class CreateDraftClaimEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exceptions/{caseId:guid}/claims", async (
            [FromRoute] Guid caseId,
            [FromBody] CreateDraftClaimRequest request,
            [FromServices] CreateDraftClaimHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateDraftClaimCommand(
                caseId,
                request.CarrierId,
                request.ClaimType,
                request.Currency);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                success => Results.Created($"/api/claims/{success.Id}", success),
                error => Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("CreateDraftClaim")
        .WithTags("Claims");
    }
}
