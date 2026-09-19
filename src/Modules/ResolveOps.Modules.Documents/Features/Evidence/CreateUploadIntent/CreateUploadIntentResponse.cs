namespace ResolveOps.Modules.Documents.Features.Evidence.CreateUploadIntent;

public sealed record CreateUploadIntentResponse(
    Guid DocumentId,
    string UploadMethod,
    string UploadUrl,
    DateTimeOffset ExpiresAt,
    string StorageContainer,
    string StorageObjectName);
