using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Documents;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Documents.Features.Evidence.CreateDownloadIntent;

internal sealed class CreateDownloadIntentHandler
{
    private static readonly TimeSpan _downloadUrlExpiry = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _dbContext;
    private readonly IObjectStorageService _objectStorage;
    private readonly TimeProvider _timeProvider;

    public CreateDownloadIntentHandler(
        AppDbContext dbContext,
        IObjectStorageService objectStorage,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _objectStorage = objectStorage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateDownloadIntentResponse>> HandleAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var doc = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (doc is null)
        {
            return DomainError.Failure("ERR_DOCUMENT_NOT_FOUND", $"Evidence document '{documentId}' was not found.");
        }

        if (doc.Status != DocumentStatus.Available)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_NOT_AVAILABLE",
                $"Document '{documentId}' is not Available (current status: '{doc.Status}'). Only Available documents can be downloaded.");
        }

        // Generate short-lived presigned GET URL — never a permanent public URL.
        var url = await _objectStorage.GenerateDownloadPresignedUrlAsync(
            doc.StorageContainer,
            doc.StorageObjectName,
            _downloadUrlExpiry,
            doc.OriginalFileName,
            cancellationToken);

        return new CreateDownloadIntentResponse(
            DocumentId: doc.Id,
            DownloadUrl: url,
            ExpiresAt: _timeProvider.GetUtcNow().Add(_downloadUrlExpiry),
            OriginalFileName: doc.OriginalFileName,
            ContentType: doc.ContentType);
    }
}
