using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResolveOps.Domain.Messaging;
using ResolveOps.Messaging.Options;
using ResolveOps.Persistence;

namespace ResolveOps.Messaging;

/// <summary>
/// Background service that polls the outbox table and publishes pending messages to RabbitMQ (spec §17 / Phase 5).
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OutboxOptions> _options;
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
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(_options.Value.PollIntervalSeconds);
        _logStarted(_logger, pollInterval, null);

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
            catch (Exception ex)
            {
                _logUnexpected(_logger, ex);
            }

            await Task.Delay(pollInterval, stoppingToken);
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
            .Take(_options.Value.BatchSize)
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
        var maxAttempts = _options.Value.MaxAttempts;

        try
        {
            var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(message.Payload);
            if (envelope is null)
            {
                message.MarkFailed("Payload could not be deserialized to IntegrationEventEnvelope.", _timeProvider.GetUtcNow(), maxAttempts);
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
            message.MarkFailed(ex.Message, nextAttempt, maxAttempts);

            _logFailed(_logger, message.EventType, message.Id, message.ProcessingAttempts, maxAttempts, nextAttempt, ex);
        }
#pragma warning restore CA1031
    }
}
