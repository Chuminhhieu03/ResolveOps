namespace ResolveOps.Domain.Messaging;

/// <summary>
/// Transactional outbox message (spec §15.11 / Phase 5).
///
/// Written in the same database transaction as the business aggregate change.
/// The OutboxPublisherService (Worker) polls for Pending rows and publishes to RabbitMQ.
///
/// Processing states:
///   Pending  → being picked up or waiting
///   Processed → successfully published to broker
///   Failed   → max retries exceeded; requires manual intervention
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    /// <summary>Null for system-level events that are not tenant-scoped.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Fully qualified integration event type name, e.g. "ShipmentCreatedV1".</summary>
    public string EventType { get; private set; } = string.Empty;

    public int EventVersion { get; private set; }

    /// <summary>JSON-serialized <see cref="ResolveOps.Domain.Messaging.IntegrationEventEnvelope"/>.</summary>
    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? CausationId { get; private set; }

    /// <summary>Used for RabbitMQ routing; defaults to EventType when null.</summary>
    public string? PartitionKey { get; private set; }

    public string ProcessingStatus { get; private set; } = OutboxProcessingStatus.Pending;
    public int ProcessingAttempts { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public string? LastError { get; private set; }

    // EF Core requires a parameterless constructor.
    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid? tenantId,
        string eventType,
        int eventVersion,
        string payload,
        DateTimeOffset occurredAtUtc,
        string correlationId,
        string? causationId = null,
        string? partitionKey = null)
    {
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EventType = eventType,
            EventVersion = eventVersion,
            Payload = payload,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = correlationId,
            CausationId = causationId,
            PartitionKey = partitionKey,
            ProcessingStatus = OutboxProcessingStatus.Pending,
            ProcessingAttempts = 0,
            NextAttemptAtUtc = occurredAtUtc,
        };
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessingStatus = OutboxProcessingStatus.Processed;
        ProcessedAtUtc = processedAt;
        ProcessingAttempts++;
    }

    public void MarkFailed(string error, DateTimeOffset nextAttemptAt, int maxAttempts)
    {
        ProcessingAttempts++;
        LastError = error;

        if (ProcessingAttempts >= maxAttempts)
        {
            ProcessingStatus = OutboxProcessingStatus.Failed;
            NextAttemptAtUtc = null;
        }
        else
        {
            ProcessingStatus = OutboxProcessingStatus.Pending;
            NextAttemptAtUtc = nextAttemptAt;
        }
    }
}

public static class OutboxProcessingStatus
{
    public const string Pending = "Pending";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
