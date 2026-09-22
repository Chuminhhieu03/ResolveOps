using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public sealed class RecordDecisionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/record-decision", async (
            Guid claimId,
            [FromBody] RecordDecisionRequest request,
            IValidator<RecordDecisionCommand> validator,
            RecordDecisionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var recordedBy = httpContext.GetUserId();
            var command = new RecordDecisionCommand(
                claimId,
                request.Decision,
                request.ApprovedAmount,
                request.ReasonCodes,
                request.CarrierReference,
                request.Notes,
                request.ResponseAtUtc,
                recordedBy,
                request.SourceChannel);

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
        .RequireAuthorization(AuthorizationPolicies.RequireRecordCarrierDecision)
        .WithName("RecordClaimDecision")
        .WithTags("Claims");
    }
}
