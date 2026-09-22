using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.RemoveDocument;

/// <summary>
/// POST /api/evidence/{documentId}/remove (spec §10.4 invariant 7)
///
/// Soft-removes the document. LegalHold must be false.
/// The DB row is retained; status transitions to Removed.
/// </summary>
public sealed class RemoveDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/evidence/{documentId}/remove", async (
            Guid documentId,
            RemoveDocumentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = httpContext.TryGetUserId();
            var result = await handler.HandleAsync(documentId, currentUserId, cancellationToken);

            return result.Match(
                onSuccess: _ => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("RemoveEvidenceDocument")
        .WithTags("Evidence")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUploadEvidence);
    }
}
