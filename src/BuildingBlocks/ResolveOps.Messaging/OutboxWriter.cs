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

    public void Write(IIntegrationEvent integrationEvent, string? causationId = null)
    {
        var payload = JsonSerializer.Serialize(integrationEvent);

        var outboxMessage = OutboxMessage.Create(
            tenantId: integrationEvent.TenantId,
            eventType: integrationEvent.EventType,
            eventVersion: integrationEvent.EventVersion,
            payload: payload,
            occurredAtUtc: integrationEvent.OccurredAtUtc,
            correlationId: integrationEvent.CorrelationId,
            causationId: causationId,
            partitionKey: integrationEvent.TenantId?.ToString());

        _dbContext.OutboxMessages.Add(outboxMessage);
    }
}
