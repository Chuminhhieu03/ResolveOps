using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Documents.Features.Evidence.ListCaseEvidence;

internal sealed class ListCaseEvidenceHandler
{
    private readonly AppDbContext _dbContext;

    public ListCaseEvidenceHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListCaseEvidenceResponse>> HandleAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        // Tenant query filter enforced by EF Core.
        var caseExists = await _dbContext.ExceptionCases
            .AnyAsync(c => c.Id == caseId, cancellationToken);

        if (!caseExists)
        {
            return DomainError.Failure("ERR_CASE_NOT_FOUND", $"Exception case '{caseId}' was not found.");
        }

        var docs = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .Where(d => d.CaseId == caseId && d.Status != DocumentStatus.Removed)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new CaseEvidenceSummary(
                d.Id,
                d.EvidenceType,
                d.Status,
                d.ScanStatus,
                d.OriginalFileName,
                d.ContentType,
                d.SizeBytes,
                d.VersionNumber,
                d.SupersedesDocumentId,
                d.UploadedBy,
                d.UploadedAtUtc,
                d.ScanCompletedAtUtc,
                d.LegalHold))
            .ToListAsync(cancellationToken);

        return new ListCaseEvidenceResponse(docs);
    }
}
