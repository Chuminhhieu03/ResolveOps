using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResolveOps.Application.Documents;
using ResolveOps.Domain.Documents;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Services;

/// <summary>
/// Document processing background worker (spec §18.1, §10.4).
///
/// Polls for EvidenceDocuments in PendingScan status and for each:
/// 1. Verifies the object exists in MinIO.
/// 2. Streams the object to compute and verify the SHA-256 checksum.
/// 3. Checks for duplicate checksum within the same tenant.
/// 4. Runs the malware scanner (outside any DB transaction — spec §12.5).
/// 5. Updates document status: Available / Quarantined / Rejected.
/// 6. If Available: emits EvidenceAvailableV1 via outbox.
///
/// Assumption A-021: uses DB polling rather than RabbitMQ queue consumer.
/// Queue-based processing is deferred to Phase 16.
/// </summary>
public sealed class DocumentProcessingWorker : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private const int _batchSize = 10;

    // ── LoggerMessage delegates (CA1848) ──────────────────────────────────────

    private static readonly Action<ILogger, Exception?> _logWorkerStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "WorkerStarted"), "DocumentProcessingWorker started");

    private static readonly Action<ILogger, Exception?> _logWorkerStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "WorkerStopped"), "DocumentProcessingWorker stopped");

    private static readonly Action<ILogger, Exception?> _logWorkerError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, "WorkerError"), "DocumentProcessingWorker encountered an unhandled error");

    private static readonly Action<ILogger, int, Exception?> _logProcessingBatch =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(4, "ProcessingBatch"), "DocumentProcessingWorker processing {Count} documents");

    private static readonly Action<ILogger, Guid, Exception?> _logObjectNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(5, "ObjectNotFound"), "Document {DocumentId} object not found in storage. Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string, string, Exception?> _logChecksumMismatch =
        LoggerMessage.Define<Guid, string, string>(LogLevel.Warning, new EventId(6, "ChecksumMismatch"),
            "Document {DocumentId} checksum mismatch. Client: {ClientSha256}, Computed: {ComputedSha256}. Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logDuplicateChecksum =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(7, "DuplicateChecksum"),
            "Document {DocumentId} is a duplicate of an existing Available document (SHA-256: {Sha256}). Marking as Rejected.");

    private static readonly Action<ILogger, Guid, string?, Exception?> _logMalwareDetected =
        LoggerMessage.Define<Guid, string?>(LogLevel.Warning, new EventId(8, "MalwareDetected"),
            "Document {DocumentId} failed malware scan. Threat: {ThreatName}. Quarantining.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logScanFailed =
        LoggerMessage.Define<Guid, string>(LogLevel.Error, new EventId(9, "ScanFailed"),
            "Failed to mark document {DocumentId} as clean: {Error}");

    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logDocumentAvailable =
        LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(10, "DocumentAvailable"),
            "Document {DocumentId} is now Available (TenantId={TenantId}, EvidenceType={EvidenceType})");

    private static readonly Action<ILogger, Guid, Exception?> _logProcessDocumentError =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(11, "ProcessDocumentError"), "Error processing document {DocumentId}");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logWorkerStarted(_logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logWorkerError(_logger, ex);
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logWorkerStopped(_logger, null);
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        var malwareScanner = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Load a batch of PendingScan documents (ignore query filters — worker scans all tenants).
        var documents = await db.EvidenceDocuments
            .IgnoreQueryFilters()
            .Where(d => d.Status == DocumentStatus.PendingScan)
            .OrderBy(d => d.UploadedAtUtc)
            .Take(_batchSize)
            .ToListAsync(stoppingToken);

        if (documents.Count == 0)
        {
            return;
        }

        _logProcessingBatch(_logger, documents.Count, null);

        foreach (var doc in documents)
        {
            await ProcessSingleDocumentAsync(doc, db, objectStorage, malwareScanner, outboxWriter, timeProvider, stoppingToken);
        }
    }

    private async Task ProcessSingleDocumentAsync(
        EvidenceDocument doc,
        AppDbContext db,
        IObjectStorageService objectStorage,
        IMalwareScanner malwareScanner,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider,
        CancellationToken stoppingToken)
    {
        using var activity = DocumentMetrics.ActivitySource.StartActivity(
            "DocumentProcessingWorker.ProcessDocument",
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
                stoppingToken);

            if (!exists)
            {
                _logObjectNotFound(_logger, doc.Id, null);

                var failResult = doc.MarkScanFailed(now);
                if (failResult.IsSuccess)
                {
                    await db.SaveChangesAsync(stoppingToken);
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
                    stoppingToken);

                // Buffer the stream for both hash computation and malware scanning.
                using var buffer = new MemoryStream();
                await contentStream.CopyToAsync(buffer, stoppingToken);
                buffer.Position = 0;

                computedSha256 = ComputeSha256(buffer);

                // 3. Verify checksum matches the one the client provided.
                if (!string.IsNullOrWhiteSpace(doc.Sha256) &&
                    !string.Equals(doc.Sha256, computedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    _logChecksumMismatch(_logger, doc.Id, doc.Sha256, computedSha256, null);

                    var failResult = doc.MarkScanFailed(now);
                    if (failResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    DocumentMetrics.RejectedTotal.Add(1);
                    return;
                }

                // 4. Duplicate checksum check within the same tenant (spec §10.4 invariant 5).
                var isDuplicate = await db.EvidenceDocuments
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        d => d.TenantId == doc.TenantId
                          && d.Sha256 == computedSha256
                          && d.Id != doc.Id
                          && d.Status == DocumentStatus.Available,
                        stoppingToken);

                if (isDuplicate)
                {
                    _logDuplicateChecksum(_logger, doc.Id, computedSha256, null);

                    var failResult = doc.MarkScanFailed(now);
                    if (failResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    DocumentMetrics.RejectedTotal.Add(1);
                    return;
                }

                // 5. Malware scan (must be OUTSIDE any database transaction — spec §12.5).
                buffer.Position = 0;
                var sw = Stopwatch.StartNew();
                var scanResult = await malwareScanner.ScanAsync(buffer, doc.OriginalFileName, stoppingToken);
                sw.Stop();

                DocumentMetrics.ScannedTotal.Add(1);
                DocumentMetrics.ScanDurationMs.Record(sw.Elapsed.TotalMilliseconds);

                if (!scanResult.IsClean)
                {
                    _logMalwareDetected(_logger, doc.Id, scanResult.ThreatName, null);

                    var malResult = doc.MarkScanMalicious(now);
                    if (malResult.IsSuccess)
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    DocumentMetrics.QuarantinedTotal.Add(1);
                    return;
                }

                // 6. Mark Available and emit outbox event.
                doc.SetVerifiedSha256(computedSha256);
                var cleanResult = doc.MarkScanClean(now);

                if (!cleanResult.IsSuccess)
                {
                    _logScanFailed(_logger, doc.Id, cleanResult.Error?.Message ?? "Unknown", null);
                    return;
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
                await db.SaveChangesAsync(stoppingToken);

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
