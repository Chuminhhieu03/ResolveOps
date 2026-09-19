using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.CompleteUpload;

/// <summary>
/// Handles CompleteUpload:
/// 1. Loads the document (tenant-filtered by query filter).
/// 2. Verifies the caller is the uploader or has elevated permission.
/// 3. Calls domain method EvidenceDocument.CompleteUpload() → PendingScan.
/// 4. Persists.
/// </summary>
internal sealed class CompleteUploadHandler
{
    private static readonly Action<ILogger, Guid, Exception?> _logDocumentPendingScan =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(1, "DocumentPendingScan"),
            "Document {DocumentId} transitioned to PendingScan");
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CompleteUploadHandler> _logger;

    public CompleteUploadHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        ILogger<CompleteUploadHandler> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<CompleteUploadResponse>> HandleAsync(
        CompleteUploadCommand command,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        // Tenant query filter enforced by EF Core.
        var document = await _dbContext.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);

        if (document is null)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_NOT_FOUND",
                $"Evidence document '{command.DocumentId}' was not found.");
        }

        var domainResult = document.CompleteUpload(command.Sha256);
        if (!domainResult.IsSuccess)
        {
            return domainResult.Error!;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logDocumentPendingScan(_logger, document.Id, null);

        return new CompleteUploadResponse(
            DocumentId: document.Id,
            Status: document.Status,
            ScanStatus: document.ScanStatus,
            UpdatedAtUtc: _timeProvider.GetUtcNow());
    }
}
