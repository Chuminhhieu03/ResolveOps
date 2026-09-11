using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResolveOps.Domain.Messaging;
using ResolveOps.Persistence;

namespace ResolveOps.Messaging;

/// <summary>
/// Background service that polls the outbox table and publishes pending messages to RabbitMQ (spec §17 / Phase 5).
///
/// Design decisions:
/// - Polls on a fixed interval (default 5 seconds). Phase 6+ may switch to a DB notification
///   or shorter interval once tracking ingestion volumes are measured.
/// - Processes messages in batches of 50 per poll cycle.
/// - Applies exponential back-off on publish failures up to MaxAttempts (5).
///   After MaxAttempts the row is marked Failed and requires manual intervention.
/// - Each poll cycle opens a new DbContext scope to avoid stale query caches.
/// - Does not use a distributed lock — the outbox table has no "claimed" state in Phase 5.
///   With a single Worker instance this is safe. Phase 16 adds concurrency controls.
/// - CancellationToken is respected on every I/O operation (spec §0.1 rule 15).
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxPublisherService> _logger;

    // ── LoggerMessage delegates (CA1848 compliance) ───────────────────────
    private static readonly Action<ILogger, TimeSpan, Exception?> _logStarted =
        LoggerMessage.Define<TimeSpan>(LogLevel.Information, new EventId(1, "OutboxStarted"),
            "OutboxPublisherService started. Poll interval: {PollInterval}");

    private static readonly Action<ILogger, Exception?> _logStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "OutboxStopped"),
            "OutboxPublisherService stopped.");

    private static readonly Action<ILogger, int, Exception?> _logProcessing =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(3, "OutboxProcessing"),
            "OutboxPublisherService: processing {Count} messages.");

    private static readonly Action<ILogger, Exception?> _logUnexpected =
        LoggerMessage.Define(LogLevel.Error, new EventId(4, "OutboxUnexpectedError"),
            "OutboxPublisherService: unexpected error during poll cycle.");

    private static readonly Action<ILogger, string, Guid, Exception?> _logPublished =
        LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(5, "OutboxPublished"),
            "Outbox: published {EventType} (id={MessageId})");

    private static readonly Action<ILogger, string, Guid, int, int, DateTimeOffset, Exception?> _logFailed =
        LoggerMessage.Define<string, Guid, int, int, DateTimeOffset>(LogLevel.Warning, new EventId(6, "OutboxFailed"),
            "Outbox: failed to publish {EventType} (id={MessageId}), attempt {Attempts}/{MaxAttempts}. Next attempt at {NextAttempt}.");

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        RabbitMqPublisher publisher,
        TimeProvider timeProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logStarted(_logger, PollInterval, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — do not log as error.
                break;
            }
#pragma warning disable CA1031 // Intentional: publisher must not crash the host on transient failures.
            catch (Exception ex)
            {
                _logUnexpected(_logger, ex);
            }
#pragma warning restore CA1031

            await Task.Delay(PollInterval, stoppingToken);
        }

        _logStopped(_logger, null);
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = _timeProvider.GetUtcNow();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m =>
                m.ProcessingStatus == OutboxProcessingStatus.Pending &&
                (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logProcessing(_logger, pendingMessages.Count, null);

        foreach (var message in pendingMessages)
        {
            await PublishMessageAsync(dbContext, message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishMessageAsync(
        AppDbContext dbContext,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(message.Payload);
            if (envelope is null)
            {
                message.MarkFailed("Payload could not be deserialized to IntegrationEventEnvelope.", _timeProvider.GetUtcNow(), MaxAttempts);
                return;
            }

            // Re-wrap with publish timestamp.
            var publishEnvelope = new IntegrationEventEnvelope
            {
                EventType = envelope.EventType,
                EventVersion = envelope.EventVersion,
                OccurredAtUtc = envelope.OccurredAtUtc,
                TenantId = envelope.TenantId,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.CausationId,
                Payload = envelope.Payload,
                PublishedAtUtc = _timeProvider.GetUtcNow(),
            };

            await _publisher.PublishAsync(publishEnvelope, cancellationToken);

            message.MarkProcessed(_timeProvider.GetUtcNow());

            _logPublished(_logger, message.EventType, message.Id, null);
        }
#pragma warning disable CA1031 // Intentional: individual message failure must not abort the whole batch.
        catch (Exception ex)
        {
            var backoffSeconds = Math.Pow(2, message.ProcessingAttempts) * 5; // 5s, 10s, 20s, 40s, 80s
            var nextAttempt = _timeProvider.GetUtcNow().AddSeconds(backoffSeconds);
            message.MarkFailed(ex.Message, nextAttempt, MaxAttempts);

            _logFailed(_logger, message.EventType, message.Id, message.ProcessingAttempts, MaxAttempts, nextAttempt, ex);
        }
#pragma warning restore CA1031
    }
}
