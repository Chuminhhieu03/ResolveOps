using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.CompleteUpload;

/// <summary>
/// POST /api/evidence/{documentId}/complete-upload (spec §16.9)
///
/// Called by the client after the presigned PUT upload has finished.
/// Transitions EvidenceDocument from PendingUpload → PendingScan.
/// </summary>
public sealed class CompleteUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/evidence/{documentId}/complete-upload", async (
            Guid documentId,
            CompleteUploadRequest request,
            IValidator<CompleteUploadCommand> validator,
            CompleteUploadHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CompleteUploadCommand(documentId, request.Sha256);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var currentUserId = httpContext.TryGetUserId();
            var result = await handler.HandleAsync(command, currentUserId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CompleteEvidenceUpload")
        .WithTags("Evidence")
        .Produces<CompleteUploadResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUploadEvidence);
    }
}

public sealed record CompleteUploadRequest(string Sha256);
