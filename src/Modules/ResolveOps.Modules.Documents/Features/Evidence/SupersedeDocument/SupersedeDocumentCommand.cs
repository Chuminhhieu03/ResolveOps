namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

public sealed record SupersedeDocumentCommand(
    Guid DocumentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateOnly? DocumentDate,
    string? Issuer);
