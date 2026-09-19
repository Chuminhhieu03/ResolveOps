namespace ResolveOps.Modules.Documents.Features.Evidence.CreateUploadIntent;

public sealed record CreateUploadIntentCommand(
    Guid CaseId,
    Guid? ClaimId,
    string EvidenceType,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateOnly? DocumentDate,
    string? Issuer);
