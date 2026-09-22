using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public sealed class RecordInformationRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/record-information-request", async (
            Guid claimId,
            [FromBody] RecordInformationRequestRequest request,
            IValidator<RecordInformationRequestCommand> validator,
            RecordInformationRequestHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var recordedBy = httpContext.GetUserId();
            var command = new RecordInformationRequestCommand(
                claimId,
                request.CarrierReference,
                request.ResponseAtUtc,
                request.Notes,
                request.ReasonCodes,
                request.InfoDeadlineAtUtc,
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
        .WithName("RecordClaimInformationRequest")
        .WithTags("Claims");
    }
}
