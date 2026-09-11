namespace ResolveOps.Domain.Tracking;

/// <summary>
/// Immutable canonical tracking event (spec §15.6).
///
/// Invariants:
/// - Insert-only entity: never updated or deleted by application code.
/// - Stores both the carrier event timestamp (occurred_at_utc) and ingestion timestamp (received_at_utc).
/// </summary>
public sealed class TrackingEvent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ShipmentId { get; private set; }
    public Guid? ShipmentLegId { get; private set; }
    public Guid CarrierId { get; private set; }
    public Guid InboundReceiptId { get; private set; }
    public string? ExternalEventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string? EventCode { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? LocationText { get; private set; }
    public decimal? Quantity { get; private set; }
    public int? PackageCount { get; private set; }
    public Guid? CorrectionOfEventId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? CausationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // EF Core requires a parameterless constructor
    private TrackingEvent() { }

    public static TrackingEvent Create(
        Guid tenantId,
        Guid shipmentId,
        Guid? shipmentLegId,
        Guid carrierId,
        Guid inboundReceiptId,
        string? externalEventId,
        string eventType,
        string? eventCode,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc,
        Guid? locationId,
        string? locationText,
        decimal? quantity,
        int? packageCount,
        Guid? correctionOfEventId,
        string correlationId,
        string? causationId,
        TimeProvider timeProvider)
    {
        return new TrackingEvent
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ShipmentId = shipmentId,
            ShipmentLegId = shipmentLegId,
            CarrierId = carrierId,
            InboundReceiptId = inboundReceiptId,
            ExternalEventId = string.IsNullOrWhiteSpace(externalEventId) ? null : externalEventId.Trim(),
            EventType = eventType.Trim(),
            EventCode = string.IsNullOrWhiteSpace(eventCode) ? null : eventCode.Trim(),
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            LocationId = locationId,
            LocationText = locationText?.Trim(),
            Quantity = quantity,
            PackageCount = packageCount,
            CorrectionOfEventId = correctionOfEventId,
            CorrelationId = correlationId,
            CausationId = causationId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
    }
}
