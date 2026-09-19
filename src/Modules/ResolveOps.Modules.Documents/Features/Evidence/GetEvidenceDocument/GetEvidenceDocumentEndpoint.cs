using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceDocument;

/// <summary>GET /api/evidence/{documentId} — returns metadata only, never file bytes.</summary>
public sealed class GetEvidenceDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/evidence/{documentId}", async (
            Guid documentId,
            GetEvidenceDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(documentId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetEvidenceDocument")
        .WithTags("Evidence")
        .Produces<EvidenceDocumentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
