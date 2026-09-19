namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published when an evidence document passes malware scanning
/// and becomes Available for business workflows (spec §17.3, §10.4).
///
/// Consumers:
/// - Claim readiness recalculator (Phase 10)
/// - Notification worker (Phase 13)
/// - Reporting projection worker (Phase 14)
/// </summary>
public sealed class EvidenceAvailableV1 : IIntegrationEvent
{
    public string EventType => "EvidenceAvailableV1";
    public int EventVersion => 1;

    // ── Business payload (spec §17.3) ─────────────────────────────────────
    public Guid DocumentId { get; init; }
    public Guid CaseId { get; init; }
    public Guid? ClaimId { get; init; }
    public string EvidenceType { get; init; } = string.Empty;
    public DateTimeOffset AvailableAtUtc { get; init; }
}
