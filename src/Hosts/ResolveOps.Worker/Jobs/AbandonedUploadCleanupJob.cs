using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ResolveOps.Domain.Documents;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Quartz.NET job that cleans up abandoned upload intents (spec §24 Phase 9, task 11).
///
/// Marks <see cref="DocumentStatus.PendingUpload"/> documents older than 24 hours as
/// <see cref="DocumentStatus.Removed"/>. No MinIO object deletion is attempted
/// (the object may never have been uploaded). Physical MinIO cleanup is deferred
/// to Phase 16. (Assumption A-020, docs/assumptions.md)
///
/// Runs every 6 hours. Stable job key: "AbandonedUploadCleanupJob".
/// </summary>
[DisallowConcurrentExecution]
public sealed class AbandonedUploadCleanupJob : IJob
{
    private static readonly TimeSpan _abandonThreshold = TimeSpan.FromHours(24);

    // ── LoggerMessage delegates (CA1848) ──────────────────────────────────────

    private static readonly Action<ILogger, DateTimeOffset, Exception?> _logJobStarted =
        LoggerMessage.Define<DateTimeOffset>(LogLevel.Information, new EventId(1, "CleanupStarted"),
            "AbandonedUploadCleanupJob started at {Start}");

    private static readonly Action<ILogger, Guid, string?, Exception?> _logRemoveFailed =
        LoggerMessage.Define<Guid, string?>(LogLevel.Warning, new EventId(2, "RemoveFailed"),
            "Could not mark abandoned document {DocumentId} as Removed: {Error}");

    private static readonly Action<ILogger, int, Exception?> _logBatchCleaned =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(3, "BatchCleaned"),
            "AbandonedUploadCleanupJob: cleaned {BatchCount} documents in this batch");

    private static readonly Action<ILogger, int, double, Exception?> _logJobCompleted =
        LoggerMessage.Define<int, double>(LogLevel.Information, new EventId(4, "CleanupCompleted"),
            "AbandonedUploadCleanupJob completed: {TotalCleaned} documents removed in {DurationMs}ms");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedUploadCleanupJob> _logger;

    public AbandonedUploadCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedUploadCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var start = timeProvider.GetUtcNow();
        _logJobStarted(_logger, start, null);

        var cutoff = start.Subtract(_abandonThreshold);

        // Batch processing — avoid loading all documents into memory.
        int totalCleaned = 0;
        const int batchSize = 200;

        while (true)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var batch = await db.EvidenceDocuments
                .IgnoreQueryFilters()
                .Where(d => d.Status == DocumentStatus.PendingUpload && d.UploadedAtUtc < cutoff)
                .OrderBy(d => d.UploadedAtUtc)
                .Take(batchSize)
                .ToListAsync(context.CancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var doc in batch)
            {
                var result = doc.MarkRemoved();
                if (!result.IsSuccess)
                {
                    _logRemoveFailed(_logger, doc.Id, result.Error?.Message, null);
                }
            }

            await db.SaveChangesAsync(context.CancellationToken);
            totalCleaned += batch.Count;

            _logBatchCleaned(_logger, batch.Count, null);

            if (batch.Count < batchSize)
            {
                break; // Last batch
            }
        }

        DocumentMetrics.AbandonedCleanedTotal.Add(totalCleaned);

        var duration = timeProvider.GetUtcNow() - start;
        _logJobCompleted(_logger, totalCleaned, duration.TotalMilliseconds, null);
    }
}
