namespace ResolveOps.Modules.Documents.Features.Evidence.CompleteUpload;

public sealed record CompleteUploadCommand(Guid DocumentId, string Sha256);

public sealed record CompleteUploadResponse(
    Guid DocumentId,
    string Status,
    string ScanStatus,
    DateTimeOffset UpdatedAtUtc);
