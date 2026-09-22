using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.AppealClaim;

public sealed class AppealClaimEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/appeal", async (
            Guid claimId,
            [FromBody] AppealClaimRequest request,
            IValidator<AppealClaimCommand> validator,
            AppealClaimHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appealedBy = Guid.TryParse(userIdStr, out var parsed) ? parsed : Guid.Empty;

            var command = new AppealClaimCommand(claimId, request.AppealReason, request.Notes, appealedBy);

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
        .WithName("AppealClaim")
        .WithTags("Claims");
    }
}
