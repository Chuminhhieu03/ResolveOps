namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published to request asynchronous normalization of an inbound tracking receipt (spec §24 Phase 6).
///
/// Consumers bind to the fanout exchange for this event type to pick up receipts for normalization.
/// </summary>
public sealed class TrackingIngestionRequestedV1 : IIntegrationEvent
{
    public string EventType => "TrackingIngestionRequestedV1";
    public int EventVersion => 1;
    public DateTimeOffset OccurredAtUtc { get; init; }
    public Guid? TenantId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;

    // ── Business payload ──────────────────────────────────────────────────
    public Guid ReceiptId { get; init; }
}
