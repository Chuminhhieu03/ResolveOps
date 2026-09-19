using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ResolveOps.Application.Documents;
using ResolveOps.Domain.Documents;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Scheduled Quartz.NET job that scans and processes uploaded documents in PendingScan status (spec §18.1, §10.4).
///
/// Features:
/// 1. Verifies the object exists in MinIO.
/// 2. Streams the object to compute and verify the SHA-256 checksum.
/// 3. Checks for duplicate checksum within the same tenant against Available documents.
/// 4. Runs the malware scanner (outside any DB transaction — spec §12.5).
/// 5. Updates document status: Available / Quarantined / Rejected.
/// 6. If Available and document supersedes an older version, transitions the superseded document to Superseded atomically.
/// 7. If Available: emits EvidenceAvailableV1 via outbox.
///
/// Runs every 10 seconds. Protected by [DisallowConcurrentExecution] to prevent overlapping scans across instances.
/// Stable job key: "DocumentProcessingScanJob".
/// </summary>
[DisallowConcurrentExecution]
public sealed class DocumentProcessingScanJob : IJob
{
    private const int _batchSize = 10;

    // ── LoggerMessage delegates (CA1848) ──────────────────────────────────────

    private static readonly Action<ILogger, int, Exception?> _logProcessingBatch =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, "ProcessingBatch"), "DocumentProcessingScanJob processing {Count} documents");

    private static readonly Action<ILogger, Guid, Exception?> _logObjectNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2, "ObjectNotFound"), "Document {DocumentId} object not found in storage. Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string, string, Exception?> _logChecksumMismatch =
        LoggerMessage.Define<Guid, string, string>(LogLevel.Warning, new EventId(3, "ChecksumMismatch"),
            "Document {DocumentId} checksum mismatch. Client: {ClientSha256}, Computed: {ComputedSha256}. Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logDuplicateChecksum =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(4, "DuplicateChecksum"),
            "Document {DocumentId} is a duplicate of an existing Available document (SHA-256: {Sha256}). Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string?, Exception?> _logMalwareDetected =
        LoggerMessage.Define<Guid, string?>(LogLevel.Warning, new EventId(5, "MalwareDetected"),
            "Document {DocumentId} failed malware scan. Threat: {ThreatName}. Quarantining.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logScanFailed =
        LoggerMessage.Define<Guid, string>(LogLevel.Error, new EventId(6, "ScanFailed"),
            "Failed to mark document {DocumentId} as clean: {Error}");

    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logDocumentAvailable =
        LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(7, "DocumentAvailable"),
            "Document {DocumentId} is now Available (TenantId={TenantId}, EvidenceType={EvidenceType})");

    private static readonly Action<ILogger, Guid, Exception?> _logProcessDocumentError =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(8, "ProcessDocumentError"), "Error processing document {DocumentId}");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingScanJob> _logger;

    public DocumentProcessingScanJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingScanJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        var malwareScanner = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Load a batch of PendingScan documents across all tenants.
        var documents = await db.EvidenceDocuments
            .IgnoreQueryFilters()
            .Where(d => d.Status == DocumentStatus.PendingScan)
            .OrderBy(d => d.UploadedAtUtc)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return;
        }

        _logProcessingBatch(_logger, documents.Count, null);

        foreach (var doc in documents)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessSingleDocumentAsync(doc, db, objectStorage, malwareScanner, outboxWriter, timeProvider, cancellationToken);
        }
    }

    private async Task ProcessSingleDocumentAsync(
        EvidenceDocument doc,
        AppDbContext db,
        IObjectStorageService objectStorage,
        IMalwareScanner malwareScanner,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        using var activity = DocumentMetrics.ActivitySource.StartActivity(
            "DocumentProcessingScanJob.ProcessDocument",
            ActivityKind.Internal);
        activity?.SetTag("document.id", doc.Id.ToString());
        activity?.SetTag("tenant.id", doc.TenantId.ToString());

        var now = timeProvider.GetUtcNow();

        try
        {
            // 1. Verify object exists.
            var exists = await objectStorage.ObjectExistsAsync(
                doc.StorageContainer,
                doc.StorageObjectName,
                cancellationToken);

            if (!exists)
            {
                _logObjectNotFound(_logger, doc.Id, null);

                var failResult = doc.MarkScanFailed(now);
                if (failResult.IsSuccess)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                DocumentMetrics.RejectedTotal.Add(1);
                return;
            }

            // 2. Stream object and compute SHA-256.
            string computedSha256;
            Stream? contentStream = null;

            try
            {
                contentStream = await objectStorage.OpenReadStreamAsync(
                    doc.StorageContainer,
                    doc.StorageObjectName,
                    cancellationToken);

                using var buffer = new MemoryStream();
                await contentStream.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;

                computedSha256 = ComputeSha256(buffer);

                // 3. Verify checksum matches client-declared checksum.
                if (!string.IsNullOrWhiteSpace(doc.Sha256) &&
                    !string.Equals(doc.Sha256, computedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    _logChecksumMismatch(_logger, doc.Id, doc.Sha256, computedSha256, null);

                    var failResult = doc.MarkScanFailed(now);
                    if (failResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }

                    DocumentMetrics.RejectedTotal.Add(1);
                    return;
                }

                // 4. Duplicate checksum check within the same tenant against Available documents.
                var isDuplicate = await db.EvidenceDocuments
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        d => d.TenantId == doc.TenantId
                          && d.Sha256 == computedSha256
                          && d.Id != doc.Id
                          && d.Status == DocumentStatus.Available,
                        cancellationToken);

                if (isDuplicate)
                {
                    _logDuplicateChecksum(_logger, doc.Id, computedSha256, null);

                    var failResult = doc.MarkScanFailed(now);
                    if (failResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }

                    DocumentMetrics.RejectedTotal.Add(1);
                    return;
                }

                // 5. Malware scan (must be OUTSIDE any database transaction — spec §12.5).
                buffer.Position = 0;
                var sw = Stopwatch.StartNew();
                var scanResult = await malwareScanner.ScanAsync(buffer, doc.OriginalFileName, cancellationToken);
                sw.Stop();

                DocumentMetrics.ScannedTotal.Add(1);
                DocumentMetrics.ScanDurationMs.Record(sw.Elapsed.TotalMilliseconds);

                if (!scanResult.IsClean)
                {
                    _logMalwareDetected(_logger, doc.Id, scanResult.ThreatName, null);

                    var malResult = doc.MarkScanMalicious(now);
                    if (malResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }

                    DocumentMetrics.QuarantinedTotal.Add(1);
                    return;
                }

                // 6. Mark Available and apply deferred supersede.
                doc.SetVerifiedSha256(computedSha256);
                var cleanResult = doc.MarkScanClean(now);

                if (!cleanResult.IsSuccess)
                {
                    _logScanFailed(_logger, doc.Id, cleanResult.Error?.Message ?? "Unknown", null);
                    return;
                }

                // Deferred Supersede: atomically transition previous version to Superseded now that new version is Available.
                if (doc.SupersedesDocumentId.HasValue)
                {
                    var previousDoc = await db.EvidenceDocuments
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(
                            d => d.Id == doc.SupersedesDocumentId.Value && d.TenantId == doc.TenantId,
                            cancellationToken);

                    if (previousDoc is not null && previousDoc.Status == DocumentStatus.Available)
                    {
                        previousDoc.Supersede();
                    }
                }

                var integrationEvent = new EvidenceAvailableV1
                {
                    DocumentId = doc.Id,
                    CaseId = doc.CaseId,
                    ClaimId = doc.ClaimId,
                    EvidenceType = doc.EvidenceType,
                    AvailableAtUtc = now,
                };

                outboxWriter.Write(integrationEvent, doc.TenantId, $"doc-scan-{doc.Id}");
                await db.SaveChangesAsync(cancellationToken);

                _logDocumentAvailable(_logger, doc.Id, doc.TenantId, doc.EvidenceType, null);
            }
            finally
            {
                if (contentStream is not null)
                {
                    await contentStream.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logProcessDocumentError(_logger, doc.Id, ex);
        }
    }

    private static string ComputeSha256(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
