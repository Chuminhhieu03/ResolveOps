using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public sealed class RecordAcknowledgementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/record-acknowledgement", async (
            Guid claimId,
            [FromBody] RecordAcknowledgementRequest request,
            IValidator<RecordAcknowledgementCommand> validator,
            RecordAcknowledgementHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var recordedBy = httpContext.GetUserId();
            var command = new RecordAcknowledgementCommand(
                claimId,
                request.CarrierReference,
                request.ResponseAtUtc,
                request.Notes,
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
        .WithName("RecordClaimAcknowledgement")
        .WithTags("Claims");
    }
}
