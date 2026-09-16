namespace ResolveOps.Messaging.Events;

/// <summary>
/// Core integration event published when a tracking event has been accepted and normalized (spec §17.3, §24 Phase 6).
///
/// Consumers:
/// - Phase 7: Exception policy engine
/// - Phase 13: Notification policies
/// - Phase 14: Reporting projections
/// </summary>
public sealed class TrackingEventAcceptedV1 : IIntegrationEvent
{
    public string EventType => "TrackingEventAcceptedV1";
    public int EventVersion => 1;

    // ── Business payload (spec §17.3) ─────────────────────────────────────
    public Guid TrackingEventId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid? ShipmentLegId { get; init; }
    public Guid CarrierId { get; init; }
    public string NormalizedEventType { get; init; } = string.Empty;
}
