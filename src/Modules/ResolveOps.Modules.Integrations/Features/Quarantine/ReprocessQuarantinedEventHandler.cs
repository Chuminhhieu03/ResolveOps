using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Tracking;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Modules.Integrations.Carriers.DemoCarrier;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

internal sealed class ReprocessQuarantinedEventHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxWriter _outboxWriter;

    public ReprocessQuarantinedEventHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IOutboxWriter outboxWriter)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _outboxWriter = outboxWriter;
    }

    public async Task<Result<QuarantinedEventSummaryDto>> HandleAsync(
        Guid quarantinedEventId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var quarantinedEvent = await _dbContext.QuarantinedEvents
            .FirstOrDefaultAsync(q => q.Id == quarantinedEventId && q.TenantId == tenantId, cancellationToken);

        if (quarantinedEvent is null)
        {
            return DomainError.Failure(
                "ERR_QUARANTINED_EVENT_NOT_FOUND",
                $"Quarantined event with ID '{quarantinedEventId}' was not found.");
        }

        if (quarantinedEvent.Status != QuarantinedEventStatus.Quarantined)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Quarantined event cannot be reprocessed because its current status is '{quarantinedEvent.Status}'.");
        }

        var receipt = await _dbContext.InboundEventReceipts
            .FirstOrDefaultAsync(r => r.Id == quarantinedEvent.InboundReceiptId && r.TenantId == tenantId, cancellationToken);

        if (receipt is null)
        {
            return DomainError.Failure(
                "ERR_RECEIPT_NOT_FOUND",
                "Associated inbound receipt could not be found.");
        }

        var parsed = DemoCarrierAdapter.Parse(receipt.RawPayload);
        if (parsed is null)
        {
            return DomainError.Failure(
                "ERR_INVALID_PAYLOAD",
                "Raw receipt payload could not be parsed.");
        }

        // Attempt shipment matching via alias
        Guid? matchedShipmentId = null;
        Guid? matchedLegId = null;

        var alias = await _dbContext.ShipmentTrackingAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId
                     && a.AliasValue == parsed.TrackingNumber,
                cancellationToken);

        if (alias != null)
        {
            matchedShipmentId = alias.ShipmentId;
            matchedLegId = alias.ShipmentLegId;
        }
        else
        {
            // Fallback: match by ExternalReference
            var shipmentByRef = await _dbContext.Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.TenantId == tenantId
                         && s.ExternalReference == parsed.TrackingNumber,
                    cancellationToken);

            if (shipmentByRef != null)
            {
                matchedShipmentId = shipmentByRef.Id;
            }
        }

        if (matchedShipmentId is null)
        {
            return DomainError.Failure(
                "ERR_SHIPMENT_UNMATCHED",
                $"Could not match tracking number '{parsed.TrackingNumber}' to any shipment or alias.");
        }

        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == matchedShipmentId.Value && s.TenantId == tenantId, cancellationToken);

        if (shipment is null)
        {
            return DomainError.Failure(
                "ERR_SHIPMENT_NOT_FOUND",
                $"Matched shipment with ID '{matchedShipmentId.Value}' was not found.");
        }

        var eventType = parsed.EventType;
        var eventCode = parsed.EventCode;
        var occurredAt = parsed.OccurredAtUtc;
        var carrierId = receipt.CarrierId ?? Guid.Empty;

        // Persist immutable canonical TrackingEvent (spec §15.6)
        var trackingEvent = TrackingEvent.Create(
            tenantId: tenantId,
            shipmentId: shipment.Id,
            shipmentLegId: matchedLegId,
            carrierId: carrierId,
            inboundReceiptId: receipt.Id,
            externalEventId: receipt.ExternalEventId,
            eventType: eventType,
            eventCode: eventCode,
            occurredAtUtc: occurredAt,
            receivedAtUtc: receipt.ReceivedAtUtc,
            locationId: null,
            locationText: parsed.LocationText,
            quantity: parsed.Quantity,
            packageCount: parsed.PackageCount,
            correctionOfEventId: null,
            correlationId: correlationId,
            causationId: quarantinedEvent.Id.ToString(),
            timeProvider: _timeProvider);

        _dbContext.TrackingEvents.Add(trackingEvent);

        // Update shipment projection (spec §24 Phase 6 task 10)
        shipment.ApplyTrackingEvent(eventType, occurredAt, _timeProvider);

        // Update quarantine status and receipt status
        quarantinedEvent.Resolve(shipment.Id, null, _timeProvider);
        quarantinedEvent.MarkReprocessed(_timeProvider);
        receipt.MarkNormalized();

        // Write TrackingEventAcceptedV1 to outbox (spec §17.3)
        var outboxEvent = new TrackingEventAcceptedV1
        {
            OccurredAtUtc = occurredAt,
            TenantId = tenantId,
            CorrelationId = correlationId,
            TrackingEventId = trackingEvent.Id,
            ShipmentId = shipment.Id,
            ShipmentLegId = matchedLegId,
            CarrierId = carrierId,
            NormalizedEventType = eventType,
        };

        _outboxWriter.Write(outboxEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TrackingMetrics.NormalizedTotal.Add(1);

        return new QuarantinedEventSummaryDto(
            quarantinedEvent.Id,
            quarantinedEvent.InboundReceiptId,
            quarantinedEvent.ReasonCode,
            quarantinedEvent.Detail,
            quarantinedEvent.Status,
            quarantinedEvent.AssignedUserId,
            quarantinedEvent.ResolvedShipmentId,
            quarantinedEvent.ResolvedAtUtc,
            quarantinedEvent.CreatedAtUtc);
    }
}
