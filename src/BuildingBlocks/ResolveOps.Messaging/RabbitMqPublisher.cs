using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ResolveOps.Messaging;

/// <summary>
/// Publishes serialized <see cref="IntegrationEventEnvelope"/> messages to RabbitMQ (spec §17 / Phase 5).
///
/// Design decisions:
/// - Uses a durable fanout exchange per event type (routing key = EventType).
///   This allows per-event-type queues to be bound independently.
/// - Messages are persistent (delivery mode 2) to survive broker restart.
/// - The connection is injected as a singleton; channels are created per publish call
///   because IChannel is not thread-safe. For high-throughput scenarios (Phase 6+),
///   a channel pool should be introduced and justified by measurement.
/// - Does not ack/nack; the outbox publisher handles retry on publish failure.
/// </summary>
public sealed class RabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;

    // ── LoggerMessage delegate (CA1848 compliance) ────────────────────────
    private static readonly Action<ILogger, string, int, Guid?, string, Exception?> _logPublished =
        LoggerMessage.Define<string, int, Guid?, string>(LogLevel.Debug, new EventId(1, "RabbitMqPublished"),
            "Published {EventType} v{EventVersion} for tenant {TenantId} correlation {CorrelationId}");

    public RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Publishes the envelope to RabbitMQ using the event type as routing key.
    /// Throws on failure so the OutboxPublisherService can apply retry logic.
    /// </summary>
    public async Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        var routingKey = envelope.EventType;
        var json = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(json);

        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare a durable fanout exchange per event type.
        // Consumers bind their queues to this exchange.
        await channel.ExchangeDeclareAsync(
            exchange: routingKey,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = envelope.CorrelationId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers["event-type"] = routingKey;
        properties.Headers["event-version"] = envelope.EventVersion;
        if (envelope.TenantId.HasValue)
        {
            properties.Headers["tenant-id"] = envelope.TenantId.Value.ToString();
        }

        await channel.BasicPublishAsync(
            exchange: routingKey,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logPublished(_logger, envelope.EventType, envelope.EventVersion, envelope.TenantId, envelope.CorrelationId, null);
    }
}
