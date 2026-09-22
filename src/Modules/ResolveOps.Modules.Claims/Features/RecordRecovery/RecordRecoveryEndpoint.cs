using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public sealed class RecordRecoveryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var handlerDelegate = async (
            Guid claimId,
            [FromBody] RecordRecoveryRequest request,
            IValidator<RecordRecoveryCommand> validator,
            RecordRecoveryHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var recordedBy = httpContext.GetUserId();
            var command = new RecordRecoveryCommand(
                claimId,
                request.TransactionType,
                request.ExternalReference,
                request.Amount,
                request.Currency,
                request.ReceivedAtUtc,
                recordedBy,
                request.Notes);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Code switch
                {
                    "CLAIM_NOT_FOUND" => Results.NotFound(new { error.Code, error.Message }),
                    "DUPLICATE_EXTERNAL_REFERENCE" => Results.Conflict(new { error.Code, error.Message }),
                    _ => Results.BadRequest(new { error.Code, error.Message })
                }
            );
        };

        app.MapPost("/api/claims/{claimId:guid}/recovery", handlerDelegate)
            .RequireAuthorization(AuthorizationPolicies.RequireRecordRecovery)
            .WithName("RecordClaimRecovery")
            .WithTags("Claims");

        app.MapPost("/api/claims/{claimId:guid}/record-recovery", handlerDelegate)
            .RequireAuthorization(AuthorizationPolicies.RequireRecordRecovery)
            .WithName("RecordClaimRecoveryAlias")
            .WithTags("Claims");
    }
}
