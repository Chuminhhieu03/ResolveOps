using System;

namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published when a carrier claim decision is recorded (spec §17.3, §8.9).
///
/// Consumers:
/// - Recovery & settlement workflow (Phase 12)
/// - Carrier scorecard & financial reporting (Phase 14)
/// - Realtime notification & SignalR (Phase 13)
/// </summary>
public sealed class ClaimDecisionRecordedV1 : IIntegrationEvent
{
    public string EventType => "ClaimDecisionRecordedV1";
    public int EventVersion => 1;

    public Guid ClaimId { get; init; }
    public string Decision { get; init; } = string.Empty;
    public decimal ApprovedAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; init; }
}
