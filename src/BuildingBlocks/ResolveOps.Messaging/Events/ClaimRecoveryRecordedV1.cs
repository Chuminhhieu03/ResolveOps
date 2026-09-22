using System;

namespace ResolveOps.Messaging.Events;

/// <summary>
/// Integration event published when a financial recovery transaction is recorded for a claim (spec §17.3, §8.10).
/// </summary>
public sealed class ClaimRecoveryRecordedV1 : IIntegrationEvent
{
    public string EventType => "ClaimRecoveryRecordedV1";
    public int EventVersion => 1;

    public Guid ClaimId { get; init; }
    public Guid RecoveryTransactionId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string ExternalReference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
