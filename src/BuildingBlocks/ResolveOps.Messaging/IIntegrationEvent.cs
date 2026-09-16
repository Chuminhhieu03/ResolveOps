namespace ResolveOps.Messaging;

/// <summary>
/// Marker interface for domain fact events published via the outbox (spec §17 / Phase 5).
///
/// Concrete events contain only business data properties (domain fact payload).
/// Routing, tenancy, and tracing metadata are managed by <see cref="IntegrationEventEnvelope"/>.
/// </summary>
public interface IIntegrationEvent
{
    string EventType { get; }
    int EventVersion { get; }
}
