using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.GetClaimRecoveries;

public sealed class GetClaimRecoveriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/claims/{claimId:guid}/recoveries", async (
            Guid claimId,
            IValidator<GetClaimRecoveriesQuery> validator,
            GetClaimRecoveriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetClaimRecoveriesQuery(claimId);
            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CLAIM_NOT_FOUND"
                    ? Results.NotFound(new { error.Code, error.Message })
                    : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization(AuthorizationPolicies.RequireViewClaims)
        .WithName("GetClaimRecoveries")
        .WithTags("Claims");
    }
}
