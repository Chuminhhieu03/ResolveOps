namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published when a shipment is created (spec §24 Phase 4 task 8).
///
/// Version 1 — initial schema.
/// Consumers must tolerate additional optional properties in future versions.
/// Breaking changes require a new V2 event type.
/// </summary>
public sealed class ShipmentCreatedV1 : IIntegrationEvent
{
    public string EventType => "ShipmentCreatedV1";
    public int EventVersion => 1;

    // ── Business payload ──────────────────────────────────────────────────
    public Guid ShipmentId { get; init; }
    public string ExternalReference { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid OriginLocationId { get; init; }
    public Guid DestinationLocationId { get; init; }
    public DateTimeOffset PlannedPickupAtUtc { get; init; }
    public DateTimeOffset PlannedDeliveryAtUtc { get; init; }
    public int LegCount { get; init; }
}
