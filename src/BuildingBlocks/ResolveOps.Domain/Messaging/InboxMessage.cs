namespace ResolveOps.Domain.Messaging;

/// <summary>
/// Inbox idempotency record for consumer-side duplicate prevention (spec §15.11 / Phase 5).
///
/// Before processing a message from RabbitMQ, the consumer inserts a row with
/// (consumer_name, message_id). If a row already exists, the message is a duplicate
/// and must not produce another side effect.
///
/// Primary key is composite: (ConsumerName, MessageId).
/// </summary>
public sealed class InboxMessage
{
    /// <summary>Logical name of the consumer, e.g. "TrackingNormalizationConsumer".</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>Broker-assigned message identifier (delivery tag or application message ID).</summary>
    public string MessageId { get; private set; } = string.Empty;

    public Guid? TenantId { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    /// <summary>Optional hash of the processing result for deterministic replay verification.</summary>
    public string? ResultHash { get; private set; }

    // EF Core requires a parameterless constructor.
    private InboxMessage() { }

    public static InboxMessage Create(
        string consumerName,
        string messageId,
        Guid? tenantId,
        DateTimeOffset receivedAtUtc)
    {
        return new InboxMessage
        {
            ConsumerName = consumerName,
            MessageId = messageId,
            TenantId = tenantId,
            ReceivedAtUtc = receivedAtUtc,
        };
    }

    public void MarkProcessed(DateTimeOffset processedAt, string? resultHash = null)
    {
        ProcessedAtUtc = processedAt;
        ResultHash = resultHash;
    }
}
