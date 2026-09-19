namespace ResolveOps.Modules.Documents.Features.Evidence.ListCaseEvidence;

public sealed record ListCaseEvidenceResponse(IReadOnlyList<CaseEvidenceSummary> Items);

public sealed record CaseEvidenceSummary(
    Guid Id,
    string EvidenceType,
    string Status,
    string ScanStatus,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int VersionNumber,
    Guid? SupersedesDocumentId,
    Guid UploadedBy,
    DateTimeOffset UploadedAtUtc,
    DateTimeOffset? ScanCompletedAtUtc,
    bool LegalHold);
