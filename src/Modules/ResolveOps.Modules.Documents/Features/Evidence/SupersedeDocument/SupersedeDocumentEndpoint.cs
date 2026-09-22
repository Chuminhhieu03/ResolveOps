using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

/// <summary>
/// POST /api/evidence/{documentId}/supersede (spec §8.6, §10.4 invariant 6)
///
/// Uploads a new version to replace the current Available document.
/// The old document transitions to Superseded once the new revision is verified.
/// </summary>
public sealed class SupersedeDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/evidence/{documentId}/supersede", async (
            Guid documentId,
            SupersedeDocumentRequest request,
            IValidator<SupersedeDocumentCommand> validator,
            SupersedeDocumentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new SupersedeDocumentCommand(
                documentId,
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
            var result = await handler.HandleAsync(command, currentUserId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/evidence/{response.NewDocumentId}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("SupersedeEvidenceDocument")
        .WithTags("Evidence")
        .Produces<SupersedeDocumentResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUploadEvidence);
    }
}
