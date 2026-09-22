using System;

namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published when a claim is formally submitted to the carrier (spec §17.3, §8.8).
///
/// Consumers:
/// - Follow-up deadline scheduler / Quartz scanner (Phase 11)
/// - Carrier communication adapter (Phase 11)
/// - Reporting projection (Phase 14)
/// </summary>
public sealed class ClaimSubmittedV1 : IIntegrationEvent
{
    public string EventType => "ClaimSubmittedV1";
    public int EventVersion => 1;

    public Guid ClaimId { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public Guid CaseId { get; init; }
    public Guid CarrierId { get; init; }
    public decimal ClaimedAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAtUtc { get; init; }
    public DateTimeOffset DeadlineAtUtc { get; init; }
}
