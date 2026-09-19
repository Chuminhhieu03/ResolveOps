namespace ResolveOps.Modules.Documents.Features.Evidence.CreateDownloadIntent;

public sealed record CreateDownloadIntentResponse(
    Guid DocumentId,
    string DownloadUrl,
    DateTimeOffset ExpiresAt,
    string OriginalFileName,
    string ContentType);
