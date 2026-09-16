using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResolveOps.Domain.Messaging;
using ResolveOps.Domain.Tracking;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Messaging.Options;
using ResolveOps.Modules.Integrations.Carriers.DemoCarrier;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Consumers;

/// <summary>
/// Background consumer that processes tracking ingestion requests from RabbitMQ (spec §17.4, §17.5, §24 Phase 6).
///
/// Implements transactional Inbox pattern with Dead Letter Exchange (DLX) error handling:
/// 1. Inserts InboxMessage record inside a database transaction.
/// 2. If duplicate, commits and acks immediately.
/// 3. Normalizes payload: matches shipment via ShipmentTrackingAlias or ExternalReference.
/// 4. If unmatched, stores QuarantinedEvent.
/// 5. If matched, stores TrackingEvent, projects shipment milestones safely, and writes TrackingEventAcceptedV1 to outbox.
/// 6. Acks message upon successful transaction commit.
/// 7. Requeues transient failures up to RetryLimit; routes non-retryable errors to DLX.
/// </summary>
public sealed class TrackingIngestionConsumerService : BackgroundService
{
    private const string _queueName = "resolveops.tracking-ingestion";
    private const string _exchangeName = "TrackingIngestionRequestedV1";
    private const string _consumerName = "TrackingIngestionConsumer";
    private const string _dlxExchangeName = "resolveops.dlx";
    private const string _deadLetterQueueName = "resolveops.dead-letter";
    private const string _deadLetterRoutingKey = "tracking-ingestion.failed";

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<RabbitMqConsumerOptions> _options;
    private readonly ILogger<TrackingIngestionConsumerService> _logger;

    private static readonly Action<ILogger, string, Exception?> _logStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "ConsumerStarted"),
            "TrackingIngestionConsumerService started listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "ConsumerStopped"),
            "TrackingIngestionConsumerService stopped listening on queue {QueueName}");

    private static readonly Action<ILogger, string, string, Exception?> _logDuplicateInbox =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(3, "InboxDuplicate"),
            "Inbox duplicate message {MessageId} for consumer {ConsumerName} ignored.");

    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logNormalized =
        LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(4, "TrackingNormalized"),
            "Tracking event normalized: receiptId {ReceiptId}, shipmentId {ShipmentId}, eventType {EventType}");

    private static readonly Action<ILogger, Guid, string, Exception?> _logQuarantined =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(5, "TrackingQuarantined"),
            "Tracking event quarantined: receiptId {ReceiptId}, reason {ReasonCode}");

    private static readonly Action<ILogger, ulong, Exception?> _logError =
        LoggerMessage.Define<ulong>(LogLevel.Error, new EventId(6, "ConsumerError"),
            "Error processing tracking ingestion delivery {DeliveryTag}");

    private static readonly Action<ILogger, string, Exception?> _logChannelCreationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, "ChannelCreationError"),
            "Failed to create RabbitMQ channel for {QueueName}");

    private static readonly Action<ILogger, ulong, string, Exception?> _logDeadLettered =
        LoggerMessage.Define<ulong, string>(LogLevel.Warning, new EventId(8, "MessageDeadLettered"),
            "Tracking ingestion delivery {DeliveryTag} routed to DLX due to: {Reason}");

    public TrackingIngestionConsumerService(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<RabbitMqConsumerOptions> options,
        ILogger<TrackingIngestionConsumerService> logger)
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
            // 1. Declare DLX (direct)
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

            // 4. Declare main fanout exchange
            await channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // 5. Declare durable main queue with DLX arguments
            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _dlxExchangeName,
                ["x-dead-letter-routing-key"] = _deadLetterRoutingKey,
            };

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: stoppingToken);

            // 6. Bind queue to exchange
            await channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: string.Empty,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.Value.PrefetchCount,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var stopwatch = Stopwatch.StartNew();
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
                    _logError(_logger, deliveryTag, ex);

                    var isTransient = IsTransientError(ex);
                    var deathCount = GetDeathCount(ea.BasicProperties.Headers);

                    if (isTransient && deathCount < _options.Value.RetryLimit)
                    {
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                    }
                    else
                    {
                        _logDeadLettered(_logger, deliveryTag, isTransient ? "RetryLimitExceeded" : "NonRetryableError", null);
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: CancellationToken.None);
                    }
                }
                finally
                {
                    stopwatch.Stop();
                    TrackingMetrics.NormalizationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logStarted(_logger, _queueName, null);

            // Wait until cancellation requested
            var tcs = new TaskCompletionSource();
            stoppingToken.Register(() => tcs.TrySetResult());
            await tcs.Task;

            _logStopped(_logger, _queueName, null);
        }
    }

    private static bool IsTransientError(Exception ex) =>
        ex is TimeoutException
            or Microsoft.Data.SqlClient.SqlException
            or DbUpdateConcurrencyException
            or System.Net.Sockets.SocketException
            or System.IO.IOException;

    private static int GetDeathCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-death", out var xDeathObj) || xDeathObj is not IList<object?> deathList)
        {
            return 0;
        }

        long count = 0;
        foreach (var item in deathList)
        {
            if (item is IDictionary<string, object?> deathEntry &&
                deathEntry.TryGetValue("count", out var c) &&
                c is long l)
            {
                count += l;
            }
        }

        return (int)Math.Min(count, int.MaxValue);
    }

    private async Task ProcessMessageAsync(byte[] body, string? correlationId, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IntegrationEventEnvelope: payload is null.");

        var payload = JsonSerializer.Deserialize<TrackingIngestionRequestedV1>(envelope.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize TrackingIngestionRequestedV1 payload: payload is null.");

        if (payload.ReceiptId == Guid.Empty)
        {
            throw new InvalidOperationException("Invalid TrackingIngestionRequestedV1: ReceiptId is empty.");
        }

        var messageId = string.IsNullOrWhiteSpace(envelope.CorrelationId)
            ? payload.ReceiptId.ToString()
            : $"{payload.ReceiptId}_{envelope.CorrelationId}";

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
        var now = _timeProvider.GetUtcNow();

        // ── Transactional Inbox Check (spec §17.5) ───────────────────────────
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingInbox = await dbContext.InboxMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.ConsumerName == _consumerName && m.MessageId == messageId, cancellationToken);

        if (existingInbox != null)
        {
            _logDuplicateInbox(_logger, messageId, _consumerName, null);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inboxRecord = InboxMessage.Create(
            consumerName: _consumerName,
            messageId: messageId,
            tenantId: envelope.TenantId,
            receivedAtUtc: now);

        dbContext.InboxMessages.Add(inboxRecord);

        // ── Read Inbound Receipt ──────────────────────────────────────────────
        var receipt = await dbContext.InboundEventReceipts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == payload.ReceiptId, cancellationToken);

        if (receipt is null)
        {
            inboxRecord.MarkProcessed(now, resultHash: "RECEIPT_NOT_FOUND");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (receipt.ProcessingStatus == InboundReceiptStatus.Normalized)
        {
            inboxRecord.MarkProcessed(now, resultHash: "ALREADY_NORMALIZED");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        // ── Parse Event Payload ───────────────────────────────────────────────
        NormalizedCarrierEvent? parsed = null;
        if (receipt.SourceSystem == DemoCarrierAdapter.CarrierCode)
        {
            parsed = DemoCarrierAdapter.Parse(receipt.RawPayload);
        }
        else
        {
            // For MANUAL or generic ingest, deserialize JSON
            try
            {
                using var doc = JsonDocument.Parse(receipt.RawPayload);
                var root = doc.RootElement;
                var trackingNumber = root.TryGetProperty("TrackingNumber", out var tn) ? tn.GetString() : null;
                var eventType = root.TryGetProperty("EventType", out var et) ? et.GetString() : TrackingEventType.InTransit;
                var eventCode = root.TryGetProperty("EventCode", out var ec) ? ec.GetString() : null;
                var occurredAt = root.TryGetProperty("OccurredAtUtc", out var oa) ? oa.GetDateTimeOffset() : receipt.ReceivedAtUtc;
                var locationText = root.TryGetProperty("LocationText", out var loc) ? loc.GetString() : null;
                var packageCount = root.TryGetProperty("PackageCount", out var pc) && pc.TryGetInt32(out var pcv) ? pcv : (int?)null;
                var quantity = root.TryGetProperty("Quantity", out var q) && q.TryGetDecimal(out var qv) ? qv : (decimal?)null;
                var notes = root.TryGetProperty("Notes", out var n) ? n.GetString() : null;

                if (!string.IsNullOrWhiteSpace(trackingNumber))
                {
                    parsed = new NormalizedCarrierEvent(
                        ExternalEventId: receipt.ExternalEventId ?? Guid.NewGuid().ToString("N"),
                        TrackingNumber: trackingNumber,
                        EventType: eventType ?? TrackingEventType.InTransit,
                        EventCode: eventCode,
                        OccurredAtUtc: occurredAt,
                        LocationText: locationText,
                        PackageCount: packageCount,
                        Quantity: quantity,
                        Notes: notes);
                }
            }
            catch (JsonException)
            {
                parsed = null;
            }
        }

        if (parsed is null)
        {
            // Invalid payload → Quarantine
            var quarantined = QuarantinedEvent.Create(
                tenantId: receipt.TenantId,
                inboundReceiptId: receipt.Id,
                reasonCode: QuarantineReasonCodes.InvalidPayload,
                detail: "Unable to parse inbound payload into a tracking event.",
                timeProvider: _timeProvider);

            dbContext.QuarantinedEvents.Add(quarantined);
            receipt.MarkQuarantined(QuarantineReasonCodes.InvalidPayload, "Payload parse failure");

            inboxRecord.MarkProcessed(now, resultHash: "QUARANTINED_INVALID_PAYLOAD");
            TrackingMetrics.QuarantinedTotal.Add(1);
            _logQuarantined(_logger, receipt.Id, QuarantineReasonCodes.InvalidPayload, null);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        // ── Shipment Matching (spec §24 Phase 6 task 6) ───────────────────────
        Guid? matchedShipmentId = null;
        Guid? matchedLegId = null;

        var alias = await dbContext.ShipmentTrackingAliases
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == receipt.TenantId
                     && a.AliasValue == parsed.TrackingNumber,
                cancellationToken);

        if (alias != null)
        {
            matchedShipmentId = alias.ShipmentId;
            matchedLegId = alias.ShipmentLegId;
        }
        else
        {
            // Fallback: match by Shipment ExternalReference
            var shipmentByRef = await dbContext.Shipments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.TenantId == receipt.TenantId
                         && s.ExternalReference == parsed.TrackingNumber,
                    cancellationToken);

            if (shipmentByRef != null)
            {
                matchedShipmentId = shipmentByRef.Id;
            }
        }

        if (matchedShipmentId is null)
        {
            // Unmatched Shipment → Quarantine (spec §24 Phase 6 task 8)
            var quarantined = QuarantinedEvent.Create(
                tenantId: receipt.TenantId,
                inboundReceiptId: receipt.Id,
                reasonCode: QuarantineReasonCodes.UnmatchedShipment,
                detail: $"No shipment or tracking alias found for tracking number '{parsed.TrackingNumber}'.",
                timeProvider: _timeProvider);

            dbContext.QuarantinedEvents.Add(quarantined);
            receipt.MarkQuarantined(QuarantineReasonCodes.UnmatchedShipment, $"Unmatched tracking number: {parsed.TrackingNumber}");

            inboxRecord.MarkProcessed(now, resultHash: "QUARANTINED_UNMATCHED");
            TrackingMetrics.QuarantinedTotal.Add(1);
            _logQuarantined(_logger, receipt.Id, QuarantineReasonCodes.UnmatchedShipment, null);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        // ── Match Succeeded: Persist Canonical TrackingEvent and Update Shipment ──
        var shipment = await dbContext.Shipments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == matchedShipmentId.Value && s.TenantId == receipt.TenantId, cancellationToken);

        if (shipment is null)
        {
            var quarantined = QuarantinedEvent.Create(
                tenantId: receipt.TenantId,
                inboundReceiptId: receipt.Id,
                reasonCode: QuarantineReasonCodes.UnmatchedShipment,
                detail: $"Matched shipment with ID '{matchedShipmentId.Value}' could not be loaded.",
                timeProvider: _timeProvider);

            dbContext.QuarantinedEvents.Add(quarantined);
            receipt.MarkQuarantined(QuarantineReasonCodes.UnmatchedShipment, "Shipment load failure");

            inboxRecord.MarkProcessed(now, resultHash: "QUARANTINED_UNMATCHED");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var carrierId = receipt.CarrierId ?? shipment.Legs.FirstOrDefault(l => l.Id == matchedLegId)?.CarrierId ?? Guid.Empty;

        var trackingEvent = TrackingEvent.Create(
            tenantId: receipt.TenantId,
            shipmentId: shipment.Id,
            shipmentLegId: matchedLegId,
            carrierId: carrierId,
            inboundReceiptId: receipt.Id,
            externalEventId: receipt.ExternalEventId,
            eventType: parsed.EventType,
            eventCode: parsed.EventCode,
            occurredAtUtc: parsed.OccurredAtUtc,
            receivedAtUtc: receipt.ReceivedAtUtc,
            locationId: null,
            locationText: parsed.LocationText,
            quantity: parsed.Quantity,
            packageCount: parsed.PackageCount,
            correctionOfEventId: null,
            correlationId: envelope.CorrelationId,
            causationId: envelope.CausationId,
            timeProvider: _timeProvider);

        dbContext.TrackingEvents.Add(trackingEvent);

        // Safe projection of actual milestones & status without regression (spec §24 Phase 6 task 10, §26.3)
        shipment.ApplyTrackingEvent(parsed.EventType, parsed.OccurredAtUtc, _timeProvider);

        receipt.MarkNormalized();

        // Write TrackingEventAcceptedV1 to outbox for Phase 7 Exception policy engine
        var acceptedEvent = new TrackingEventAcceptedV1
        {
            TrackingEventId = trackingEvent.Id,
            ShipmentId = shipment.Id,
            ShipmentLegId = matchedLegId,
            CarrierId = carrierId,
            NormalizedEventType = parsed.EventType,
        };

        outboxWriter.Write(acceptedEvent, receipt.TenantId, envelope.CorrelationId, causationId: envelope.EventType);

        inboxRecord.MarkProcessed(now, resultHash: trackingEvent.Id.ToString());
        TrackingMetrics.NormalizedTotal.Add(1);
        _logNormalized(_logger, receipt.Id, shipment.Id, parsed.EventType, null);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
