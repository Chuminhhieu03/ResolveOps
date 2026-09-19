namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

public sealed record SupersedeDocumentResponse(
    Guid NewDocumentId,
    Guid SupersededDocumentId,
    int NewVersionNumber,
    string UploadUrl,
    DateTimeOffset ExpiresAt);
