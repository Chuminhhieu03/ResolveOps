using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceDocument;

public sealed record EvidenceDocumentResponse(
    Guid Id,
    Guid CaseId,
    Guid? ClaimId,
    string EvidenceType,
    string Status,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? Sha256,
    DateOnly? DocumentDate,
    string? Issuer,
    int VersionNumber,
    Guid? SupersedesDocumentId,
    Guid UploadedBy,
    DateTimeOffset UploadedAtUtc,
    string ScanStatus,
    DateTimeOffset? ScanCompletedAtUtc,
    DateOnly? RetentionUntil,
    bool LegalHold,
    string ConcurrencyStamp);

internal sealed class GetEvidenceDocumentHandler
{
    private readonly AppDbContext _dbContext;

    public GetEvidenceDocumentHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<EvidenceDocumentResponse>> HandleAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        // Tenant query filter enforced by EF Core.
        var doc = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (doc is null)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_NOT_FOUND",
                $"Evidence document '{documentId}' was not found.");
        }

        return new EvidenceDocumentResponse(
            Id: doc.Id,
            CaseId: doc.CaseId,
            ClaimId: doc.ClaimId,
            EvidenceType: doc.EvidenceType,
            Status: doc.Status,
            OriginalFileName: doc.OriginalFileName,
            ContentType: doc.ContentType,
            SizeBytes: doc.SizeBytes,
            Sha256: doc.Sha256,
            DocumentDate: doc.DocumentDate,
            Issuer: doc.Issuer,
            VersionNumber: doc.VersionNumber,
            SupersedesDocumentId: doc.SupersedesDocumentId,
            UploadedBy: doc.UploadedBy,
            UploadedAtUtc: doc.UploadedAtUtc,
            ScanStatus: doc.ScanStatus,
            ScanCompletedAtUtc: doc.ScanCompletedAtUtc,
            RetentionUntil: doc.RetentionUntil,
            LegalHold: doc.LegalHold,
            ConcurrencyStamp: doc.ConcurrencyStamp);
    }
}
