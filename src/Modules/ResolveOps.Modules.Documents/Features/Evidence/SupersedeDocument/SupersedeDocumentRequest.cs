namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

public sealed record SupersedeDocumentRequest(
    string FileName,
    string ContentType,
    long SizeBytes,
    DateOnly? DocumentDate = null,
    string? Issuer = null);
