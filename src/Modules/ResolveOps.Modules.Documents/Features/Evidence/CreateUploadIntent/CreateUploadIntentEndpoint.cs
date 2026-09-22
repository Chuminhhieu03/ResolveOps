using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Domain.Documents;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.CreateUploadIntent;

/// <summary>
/// POST /api/exceptions/{caseId}/evidence/upload-intents (spec §16.9)
///
/// Creates an EvidenceDocument in PendingUpload status and returns a short-lived
/// presigned PUT URL for direct client upload to MinIO. Permanent public URLs
/// are never returned (spec §10.4, §16.9).
/// </summary>
public sealed class CreateUploadIntentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exceptions/{caseId}/evidence/upload-intents", async (
            Guid caseId,
            CreateUploadIntentRequest request,
            IValidator<CreateUploadIntentCommand> validator,
            CreateUploadIntentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateUploadIntentCommand(
                caseId,
                request.ClaimId,
                request.EvidenceType,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.DocumentDate,
                request.Issuer);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var currentUserId = httpContext.TryGetUserId();
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, currentUserId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/evidence/{response.DocumentId}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateEvidenceUploadIntent")
        .WithTags("Evidence")
        .Produces<CreateUploadIntentResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireUploadEvidence);
    }
}

public sealed record CreateUploadIntentRequest(
    string EvidenceType,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? ClaimId = null,
    DateOnly? DocumentDate = null,
    string? Issuer = null);
