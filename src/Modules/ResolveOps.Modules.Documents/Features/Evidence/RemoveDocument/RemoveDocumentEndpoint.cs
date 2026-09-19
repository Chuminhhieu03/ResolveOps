using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;
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
            var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? currentUserId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : null;

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

internal sealed class RemoveDocumentHandler
{
    private readonly AppDbContext _dbContext;

    public RemoveDocumentHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid documentId,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var doc = await _dbContext.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (doc is null)
        {
            return DomainError.Failure("ERR_DOCUMENT_NOT_FOUND", $"Evidence document '{documentId}' was not found.");
        }

        var result = doc.MarkRemoved();
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
