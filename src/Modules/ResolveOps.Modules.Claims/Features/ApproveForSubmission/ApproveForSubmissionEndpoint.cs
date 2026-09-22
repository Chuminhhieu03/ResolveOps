using System;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.ApproveForSubmission;

public sealed class ApproveForSubmissionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/claims/{claimId:guid}/approve-for-submission", async (
            Guid claimId,
            [FromBody] ApproveForSubmissionRequest? request,
            IValidator<ApproveForSubmissionCommand> validator,
            ApproveForSubmissionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var approverId = httpContext.GetUserId();
            var command = new ApproveForSubmissionCommand(claimId, approverId, request?.Note);

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
                    : error.Code == "SEPARATION_OF_DUTIES_VIOLATION"
                        ? Results.Json(new { error.Code, error.Message }, statusCode: StatusCodes.Status403Forbidden)
                        : Results.BadRequest(new { error.Code, error.Message })
            );
        })
        .RequireAuthorization(AuthorizationPolicies.RequireApproveClaimSubmission)
        .WithName("ApproveClaimForSubmission")
        .WithTags("Claims");
    }
}
