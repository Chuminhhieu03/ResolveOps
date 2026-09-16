using System.Text.Json;
using ResolveOps.Domain.Messaging;
using ResolveOps.Persistence;

namespace ResolveOps.Messaging;

/// <summary>
/// EF Core-backed outbox writer (spec §17 / Phase 5).
///
/// Adds an <see cref="OutboxMessage"/> to the DbContext change tracker
/// without calling SaveChanges. The handler is responsible for calling
/// SaveChangesAsync so the outbox row and the business aggregate are
/// committed in one SQL transaction.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public OutboxWriter(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public void Write(
        IIntegrationEvent integrationEvent,
        Guid? tenantId,
        string correlationId,
        string? causationId = null)
    {
        var now = _timeProvider.GetUtcNow();
        var domainFactPayload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        var envelope = new IntegrationEventEnvelope
        {
            EventType = integrationEvent.EventType,
            EventVersion = integrationEvent.EventVersion,
            OccurredAtUtc = now,
            TenantId = tenantId,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = domainFactPayload,
        };

        var envelopePayload = JsonSerializer.Serialize(envelope);

        var outboxMessage = OutboxMessage.Create(
            tenantId: tenantId,
            eventType: integrationEvent.EventType,
            eventVersion: integrationEvent.EventVersion,
            payload: envelopePayload,
            occurredAtUtc: now,
            correlationId: correlationId,
            causationId: causationId,
            partitionKey: tenantId?.ToString());

        _dbContext.OutboxMessages.Add(outboxMessage);
    }
}
