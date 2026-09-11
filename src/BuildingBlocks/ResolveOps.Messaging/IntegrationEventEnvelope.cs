namespace ResolveOps.Messaging;

/// <summary>
/// Standard message envelope wrapping integration event payloads published to RabbitMQ (spec §17).
///
/// The envelope is serialized as the RabbitMQ message body (UTF-8 JSON).
/// Consumers deserialize the envelope, then deserialize the Payload using EventType
/// to determine the concrete C# type.
/// </summary>
public sealed class IntegrationEventEnvelope
{
    /// <summary>Stable routing key / event type, e.g. "ShipmentCreatedV1".</summary>
    public string EventType { get; init; } = string.Empty;

    public int EventVersion { get; init; }

    /// <summary>When the business event occurred (UTC). Not the publish timestamp.</summary>
    public DateTimeOffset OccurredAtUtc { get; init; }

    public Guid? TenantId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }

    /// <summary>JSON-serialized concrete event payload.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the message was published by the outbox publisher.</summary>
    public DateTimeOffset PublishedAtUtc { get; init; }
}
