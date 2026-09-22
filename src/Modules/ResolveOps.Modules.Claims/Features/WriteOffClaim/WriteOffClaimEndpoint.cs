using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public sealed class WriteOffClaimEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/write-off", async (
            Guid claimId,
            [FromBody] WriteOffClaimRequest request,
            IValidator<WriteOffClaimCommand> validator,
            WriteOffClaimHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var approvedBy = httpContext.GetUserId();
            var command = new WriteOffClaimCommand(
                claimId,
                request.WriteOffAmount,
                request.Reason,
                approvedBy);

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
        .RequireAuthorization(AuthorizationPolicies.RequireWriteOffClaim)
        .WithName("WriteOffClaim")
        .WithTags("Claims");
    }
}
