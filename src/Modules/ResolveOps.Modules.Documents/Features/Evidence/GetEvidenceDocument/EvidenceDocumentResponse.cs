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
