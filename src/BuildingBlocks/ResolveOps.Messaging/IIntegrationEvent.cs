namespace ResolveOps.Messaging;

/// <summary>
/// Marker interface for integration events published via the outbox (spec §17 / Phase 5).
///
/// Every integration event must provide:
/// - EventType: stable string identifier used as RabbitMQ routing key.
/// - EventVersion: monotonic version for schema evolution.
/// - OccurredAtUtc: when the business event happened (not when published).
/// - TenantId: required for tenant-isolated event routing.
/// - CorrelationId: for distributed tracing across services.
/// </summary>
public interface IIntegrationEvent
{
    string EventType { get; }
    int EventVersion { get; }
    DateTimeOffset OccurredAtUtc { get; }
    Guid? TenantId { get; }
    string CorrelationId { get; }
}
