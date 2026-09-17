namespace ResolveOps.Messaging.Events;

/// <summary>
/// Core integration event published when an exception case is detected and created (spec §17.3, §24 Phase 7).
///
/// Consumers:
/// - Phase 8: Workflow engine / task generator / SLA clock initializers
/// - Phase 13: Operational notifications
/// - Phase 14: Exception reporting projections
/// </summary>
public sealed class ExceptionDetectedV1 : IIntegrationEvent
{
    public string EventType => "ExceptionDetectedV1";
    public int EventVersion => 1;

    // ── Business payload (spec §17.3) ─────────────────────────────────────
    public Guid CaseId { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public Guid ShipmentId { get; init; }
    public string ExceptionType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; init; }
    public string? OwnerTeamCode { get; init; }
}
