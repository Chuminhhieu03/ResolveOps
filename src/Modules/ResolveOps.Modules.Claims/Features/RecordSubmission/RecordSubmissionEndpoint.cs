using System;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public sealed class RecordSubmissionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/record-submission", async (
            Guid claimId,
            [FromBody] RecordSubmissionRequest request,
            IValidator<RecordSubmissionCommand> validator,
            RecordSubmissionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RecordSubmissionCommand(claimId, request.ExternalReference, request.SubmittedAtUtc);

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
                    : error.Code == "DUPLICATE_SUBMISSION_REFERENCE"
                        ? Results.Conflict(new { error.Code, error.Message })
                        : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization(AuthorizationPolicies.RequireCreateClaim)
        .WithName("RecordClaimSubmission")
        .WithTags("Claims");
    }
}
