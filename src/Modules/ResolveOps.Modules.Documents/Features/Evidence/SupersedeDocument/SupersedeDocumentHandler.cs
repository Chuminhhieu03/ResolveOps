using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResolveOps.Application;
using ResolveOps.Application.Documents;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Modules.Documents.Infrastructure;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

internal sealed class SupersedeDocumentHandler
{
    private static readonly TimeSpan _uploadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly IObjectStorageService _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;

    public SupersedeDocumentHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IObjectStorageService objectStorage,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _objectStorage = objectStorage;
        _storageOptions = storageOptions.Value;
    }

    public async Task<Result<SupersedeDocumentResponse>> HandleAsync(
        SupersedeDocumentCommand command,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        if (currentUserId is null)
        {
            return DomainError.Failure("ERR_UNAUTHORIZED", "Authenticated user required.");
        }

        // Tenant filter applied by EF.
        var original = await _dbContext.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);

        if (original is null)
        {
            return DomainError.Failure("ERR_DOCUMENT_NOT_FOUND", $"Document '{command.DocumentId}' not found.");
        }

        // Validate MIME type against original evidence type allowlist.
        if (!EvidenceType.IsMimeTypeAllowed(original.EvidenceType, command.ContentType))
        {
            return DomainError.Failure(
                "ERR_VALIDATION_FAILED",
                $"Content type '{command.ContentType}' is not allowed for evidence type '{original.EvidenceType}'.");
        }

        // Build new version and link to original.
        var newDocumentId = Guid.CreateVersion7();
        var safeFileName = System.IO.Path.GetFileName(command.FileName);
        var objectName = $"tenants/{tenantId}/cases/{original.CaseId}/{original.EvidenceType.ToLowerInvariant()}/{newDocumentId:N}_{safeFileName}";

        var createResult = EvidenceDocument.CreateSupersedingDocument(
            previousDocument: original,
            newDocumentId: newDocumentId,
            originalFileName: command.FileName,
            storageObjectName: objectName,
            storageContainer: _storageOptions.BucketName,
            contentType: command.ContentType,
            sizeBytes: command.SizeBytes,
            documentDate: command.DocumentDate,
            issuer: command.Issuer,
            uploadedBy: currentUserId.Value,
            timeProvider: _timeProvider);

        if (!createResult.IsSuccess)
        {
            return createResult.Error!;
        }

        var newDoc = createResult.Value;

        // Generate presigned upload URL before saving (outside DB transaction).
        var uploadUrl = await _objectStorage.GenerateUploadPresignedUrlAsync(
            _storageOptions.BucketName,
            newDoc.StorageObjectName,
            command.ContentType,
            _uploadUrlExpiry,
            cancellationToken);

        _dbContext.EvidenceDocuments.Add(newDoc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        DocumentMetrics.UploadedTotal.Add(1);

        return new SupersedeDocumentResponse(
            NewDocumentId: newDoc.Id,
            SupersededDocumentId: original.Id,
            NewVersionNumber: newDoc.VersionNumber,
            UploadUrl: uploadUrl,
            ExpiresAt: _timeProvider.GetUtcNow().Add(_uploadUrlExpiry));
    }
}
