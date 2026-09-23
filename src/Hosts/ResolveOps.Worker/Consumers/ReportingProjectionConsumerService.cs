using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResolveOps.Domain.Messaging;
using ResolveOps.Domain.Reporting;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Messaging.Options;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Consumers;

public sealed class ReportingProjectionConsumerService : BackgroundService
{
    private const string _queueName = "resolveops.reporting";
    private const string _consumerName = "ReportingProjectionConsumer";
    private const string _dlxExchangeName = "resolveops.dlx";
    private const string _deadLetterQueueName = "resolveops.dead-letter";
    private const string _deadLetterRoutingKey = "reporting.failed";

    private static readonly string[] _monitoredExchanges =
    [
        "ShipmentCreatedV1",
        "TrackingEventAcceptedV1",
        "ExceptionDetectedV1",
        "CaseSlaBreachedV1",
        "ClaimSubmittedV1",
        "ClaimDecisionRecordedV1",
        "ClaimRecoveryRecordedV1",
        "EvidenceAvailableV1"
    ];

    private static readonly Action<ILogger, string, Exception?> _logChannelCreationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "ChannelCreationError"),
            "Failed to create RabbitMQ channel for {QueueName}");

    private static readonly Action<ILogger, ulong, Exception?> _logConsumerError =
        LoggerMessage.Define<ulong>(LogLevel.Error, new EventId(2, "ConsumerError"),
            "Error processing reporting event delivery {DeliveryTag}");

    private static readonly Action<ILogger, string, Exception?> _logStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "ConsumerStarted"),
            "ReportingProjectionConsumerService started listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, "ConsumerStopped"),
            "ReportingProjectionConsumerService stopped listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logDuplicateInbox =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5, "DuplicateInbox"),
            "Inbox duplicate message {MessageId} ignored in reporting consumer.");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnection _connection;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<RabbitMqConsumerOptions> _options;
    private readonly ILogger<ReportingProjectionConsumerService> _logger;

    public ReportingProjectionConsumerService(
        IConnection connection,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<RabbitMqConsumerOptions> options,
        ILogger<ReportingProjectionConsumerService> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel channel;
        try
        {
            channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logChannelCreationError(_logger, _queueName, ex);
            return;
        }

        await using (channel)
        {
            // 1. Declare DLX
            await channel.ExchangeDeclareAsync(
                exchange: _dlxExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // 2. Declare dead-letter queue
            await channel.QueueDeclareAsync(
                queue: _deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // 3. Bind dead-letter queue to DLX
            await channel.QueueBindAsync(
                queue: _deadLetterQueueName,
                exchange: _dlxExchangeName,
                routingKey: _deadLetterRoutingKey,
                cancellationToken: stoppingToken);

            // 4. Declare main queue with DLX arguments
            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _dlxExchangeName,
                ["x-dead-letter-routing-key"] = _deadLetterRoutingKey
            };

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: stoppingToken);

            // 5. Declare and bind monitored fanout exchanges
            foreach (var exchange in _monitoredExchanges)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: exchange,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: exchange,
                    routingKey: string.Empty,
                    cancellationToken: stoppingToken);
            }

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.Value.PrefetchCount,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var deliveryTag = ea.DeliveryTag;
                try
                {
                    await ProcessMessageAsync(ea.Body.ToArray(), ea.BasicProperties.CorrelationId, stoppingToken);
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logConsumerError(_logger, deliveryTag, ex);
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: CancellationToken.None);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logStarted(_logger, _queueName, null);

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (stoppingToken.Register(state => ((TaskCompletionSource)state!).TrySetResult(), tcs))
            {
                await tcs.Task;
            }

            _logStopped(_logger, _queueName, null);
        }
    }

    private async Task ProcessMessageAsync(byte[] body, string? correlationId, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json, _jsonOptions);
        if (envelope == null || !envelope.TenantId.HasValue)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messageId = !string.IsNullOrEmpty(correlationId) ? correlationId : envelope.CorrelationId;
        if (string.IsNullOrEmpty(messageId))
        {
            messageId = Guid.NewGuid().ToString("N");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Deduplication via Inbox
        var existingInbox = await dbContext.InboxMessages
            .FirstOrDefaultAsync(m => m.ConsumerName == _consumerName && m.MessageId == messageId, cancellationToken);

        if (existingInbox != null)
        {
            _logDuplicateInbox(_logger, messageId, null);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inboxRecord = InboxMessage.Create(
            _consumerName,
            messageId,
            envelope.TenantId,
            _timeProvider.GetUtcNow());

        inboxRecord.MarkProcessed(_timeProvider.GetUtcNow());
        dbContext.InboxMessages.Add(inboxRecord);

        // Update read model projection
        var tenantId = envelope.TenantId.Value;

        switch (envelope.EventType)
        {
            case "ShipmentCreatedV1":
                await HandleShipmentCreatedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            case "TrackingEventAcceptedV1":
                await HandleTrackingEventAcceptedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            case "ExceptionDetectedV1":
                await HandleExceptionDetectedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            case "ClaimSubmittedV1":
                await HandleClaimSubmittedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            case "ClaimDecisionRecordedV1":
                await HandleClaimDecisionRecordedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            case "ClaimRecoveryRecordedV1":
                await HandleClaimRecoveryRecordedAsync(dbContext, tenantId, envelope, cancellationToken);
                break;

            default:
                // Other events tracked for telemetry
                break;
        }

        ReportingMetrics.ProjectionsProcessedTotal.Add(1,
            new KeyValuePair<string, object?>("event_type", envelope.EventType));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleShipmentCreatedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ShipmentCreatedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var leg = await dbContext.ShipmentLegs
            .FirstOrDefaultAsync(l => l.ShipmentId == evt.ShipmentId && l.SequenceNumber == 1, ct);

        if (leg != null && leg.CarrierId != Guid.Empty)
        {
            var periodDate = DateOnly.FromDateTime(envelope.OccurredAtUtc.UtcDateTime);
            var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, leg.CarrierId, periodDate, ct);
            snapshot.RecordShipment(_timeProvider);
        }
    }

    private async Task HandleTrackingEventAcceptedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TrackingEventAcceptedV1>(envelope.Payload, _jsonOptions);
        if (evt == null || evt.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(envelope.OccurredAtUtc.UtcDateTime);
        var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, evt.CarrierId, periodDate, ct);

        var isDelayed = evt.NormalizedEventType.Contains("Delayed", StringComparison.OrdinalIgnoreCase);
        var isOnTime = evt.NormalizedEventType.Contains("Delivered", StringComparison.OrdinalIgnoreCase) && !isDelayed;

        snapshot.RecordTrackingEvent(isDelayed, isOnTime, _timeProvider);
    }

    private async Task HandleExceptionDetectedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ExceptionDetectedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var leg = await dbContext.ShipmentLegs
            .FirstOrDefaultAsync(l => l.ShipmentId == evt.ShipmentId && l.SequenceNumber == 1, ct);

        if (leg != null && leg.CarrierId != Guid.Empty)
        {
            var periodDate = DateOnly.FromDateTime(evt.DetectedAtUtc.UtcDateTime);
            var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, leg.CarrierId, periodDate, ct);
            snapshot.RecordException(evt.Severity, _timeProvider);
        }
    }

    private async Task HandleClaimSubmittedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimSubmittedV1>(envelope.Payload, _jsonOptions);
        if (evt == null || evt.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.SubmittedAtUtc.UtcDateTime);
        var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, evt.CarrierId, periodDate, ct);
        snapshot.RecordClaimSubmitted(evt.ClaimedAmount, _timeProvider);
    }

    private async Task HandleClaimDecisionRecordedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimDecisionRecordedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == evt.ClaimId, ct);
        if (claim == null || claim.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.RecordedAtUtc.UtcDateTime);
        var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, claim.CarrierId, periodDate, ct);

        double? responseTime = claim.SubmittedAtUtc.HasValue
            ? (evt.RecordedAtUtc - claim.SubmittedAtUtc.Value).TotalHours
            : null;

        snapshot.RecordClaimDecision(evt.Decision, evt.ApprovedAmount, responseTime, _timeProvider);
    }

    private async Task HandleClaimRecoveryRecordedAsync(
        AppDbContext dbContext,
        Guid tenantId,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimRecoveryRecordedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == evt.ClaimId, ct);
        if (claim == null || claim.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.ReceivedAtUtc.UtcDateTime);
        var snapshot = await GetOrCreateSnapshotAsync(dbContext, tenantId, claim.CarrierId, periodDate, ct);
        snapshot.RecordClaimRecovery(evt.Amount, _timeProvider);
    }

    private async Task<CarrierPerformanceSnapshot> GetOrCreateSnapshotAsync(
        AppDbContext dbContext,
        Guid tenantId,
        Guid carrierId,
        DateOnly periodDate,
        CancellationToken ct)
    {
        var snapshot = await dbContext.CarrierPerformanceSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.CarrierId == carrierId && s.PeriodDate == periodDate, ct);

        if (snapshot != null)
        {
            return snapshot;
        }

        var carrier = await dbContext.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId, ct);
        var carrierName = carrier?.Name ?? "Carrier";

        snapshot = CarrierPerformanceSnapshot.Create(tenantId, carrierId, carrierName, periodDate, _timeProvider);
        dbContext.CarrierPerformanceSnapshots.Add(snapshot);
        return snapshot;
    }
}
