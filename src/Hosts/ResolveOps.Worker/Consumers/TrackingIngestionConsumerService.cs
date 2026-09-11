using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResolveOps.Domain.Messaging;
using ResolveOps.Domain.Tracking;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Modules.Integrations.Carriers.DemoCarrier;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Consumers;

/// <summary>
/// Background consumer that processes tracking ingestion requests from RabbitMQ (spec §17.4, §17.5, §24 Phase 6).
///
/// Implements transactional Inbox pattern:
/// 1. Inserts InboxMessage record inside a database transaction.
/// 2. If duplicate, commits and acks immediately.
/// 3. Normalizes payload: matches shipment via ShipmentTrackingAlias or ExternalReference.
/// 4. If unmatched, stores QuarantinedEvent.
/// 5. If matched, stores TrackingEvent, projects shipment milestones safely, and writes TrackingEventAcceptedV1 to outbox.
/// 6. Acks message upon successful transaction commit.
/// </summary>
public sealed class TrackingIngestionConsumerService : BackgroundService
{
    private const string QueueName = "resolveops.tracking-ingestion";
    private const string ExchangeName = "TrackingIngestionRequestedV1";
    private const string ConsumerName = "TrackingIngestionConsumer";

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
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

    public TrackingIngestionConsumerService(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<TrackingIngestionConsumerService> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
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
            _logChannelCreationError(_logger, QueueName, ex);
            return;
        }

        await using (channel)
        {
            // Declare exchange
            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Declare durable queue
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // Bind queue to exchange
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: string.Empty,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

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
#pragma warning disable CA1031 // Do not let worker crash on unhandled message exception
                catch (Exception ex)
                {
                    _logError(_logger, deliveryTag, ex);
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
#pragma warning restore CA1031
                finally
                {
                    stopwatch.Stop();
                    TrackingMetrics.NormalizationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logStarted(_logger, QueueName, null);

            // Wait until cancellation requested
            var tcs = new TaskCompletionSource();
            stoppingToken.Register(() => tcs.TrySetResult());
            await tcs.Task;

            _logStopped(_logger, QueueName, null);
        }
    }

    private async Task ProcessMessageAsync(byte[] body, string? correlationId, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);
        IntegrationEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope is null)
        {
            return;
        }

        TrackingIngestionRequestedV1? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TrackingIngestionRequestedV1>(envelope.Payload);
        }
        catch (JsonException)
        {
            return;
        }

        if (payload is null || payload.ReceiptId == Guid.Empty)
        {
            return;
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
            .FirstOrDefaultAsync(m => m.ConsumerName == ConsumerName && m.MessageId == messageId, cancellationToken);

        if (existingInbox != null)
        {
            _logDuplicateInbox(_logger, messageId, ConsumerName, null);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inboxRecord = InboxMessage.Create(
            consumerName: ConsumerName,
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
            OccurredAtUtc = parsed.OccurredAtUtc,
            TenantId = receipt.TenantId,
            CorrelationId = envelope.CorrelationId,
            TrackingEventId = trackingEvent.Id,
            ShipmentId = shipment.Id,
            ShipmentLegId = matchedLegId,
            CarrierId = carrierId,
            NormalizedEventType = parsed.EventType,
        };

        outboxWriter.Write(acceptedEvent);

        inboxRecord.MarkProcessed(now, resultHash: trackingEvent.Id.ToString());
        TrackingMetrics.NormalizedTotal.Add(1);
        _logNormalized(_logger, receipt.Id, shipment.Id, parsed.EventType, null);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
