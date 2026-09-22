using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.CloseClaim;

public sealed class CloseClaimEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/close", async (
            Guid claimId,
            [FromBody] CloseClaimRequest request,
            IValidator<CloseClaimCommand> validator,
            CloseClaimHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var closedBy = httpContext.GetUserId();
            var command = new CloseClaimCommand(
                claimId,
                request.ClosingNotes,
                closedBy);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CLAIM_NOT_FOUND"
                    ? Results.NotFound(new { error.Code, error.Message })
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization(AuthorizationPolicies.RequireCloseClaim)
        .WithName("CloseClaim")
        .WithTags("Claims");
    }
}
