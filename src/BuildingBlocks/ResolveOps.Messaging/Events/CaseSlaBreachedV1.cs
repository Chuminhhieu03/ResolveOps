namespace ResolveOps.Messaging.Events;

/// <summary>
/// Core integration event published when an SLA clock breaches its target deadline (spec §17.3, §18.1, §24 Phase 8).
///
/// Consumers:
/// - Phase 13: Operational escalation notifications
/// - Phase 14: SLA performance reporting projections
/// </summary>
public sealed class CaseSlaBreachedV1 : IIntegrationEvent
{
    public string EventType => "CaseSlaBreachedV1";
    public int EventVersion => 1;

    // ── Business payload (spec §17.3) ─────────────────────────────────────
    public Guid CaseId { get; init; }
    public Guid ClockId { get; init; }
    public string ClockType { get; init; } = string.Empty;
    public DateTimeOffset BreachedAtUtc { get; init; }
    public string Severity { get; init; } = string.Empty;
}
