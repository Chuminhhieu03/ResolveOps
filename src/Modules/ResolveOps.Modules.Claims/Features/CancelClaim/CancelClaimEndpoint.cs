using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.CancelClaim;

public sealed class CancelClaimEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/cancel", async (
            Guid claimId,
            [FromBody] CancelClaimRequest request,
            IValidator<CancelClaimCommand> validator,
            CancelClaimHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cancelledBy = Guid.TryParse(userIdStr, out var parsed) ? parsed : Guid.Empty;

            var command = new CancelClaimCommand(claimId, request.Reason, cancelledBy);

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
        .RequireAuthorization(AuthorizationPolicies.RequireCreateClaim)
        .WithName("CancelClaim")
        .WithTags("Claims");
    }
}
