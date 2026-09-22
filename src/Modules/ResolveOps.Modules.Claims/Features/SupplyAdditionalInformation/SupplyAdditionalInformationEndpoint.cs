using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.SupplyAdditionalInformation;

public sealed class SupplyAdditionalInformationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/supply-additional-information", async (
            Guid claimId,
            [FromBody] SupplyAdditionalInformationRequest request,
            IValidator<SupplyAdditionalInformationCommand> validator,
            SupplyAdditionalInformationHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var recordedBy = Guid.TryParse(userIdStr, out var parsed) ? parsed : Guid.Empty;

            var command = new SupplyAdditionalInformationCommand(claimId, request.ResponseNotes, recordedBy);

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
        .WithName("SupplyClaimAdditionalInformation")
        .WithTags("Claims");
    }
}
