using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Documents.Features.Evidence.RemoveDocument;

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
