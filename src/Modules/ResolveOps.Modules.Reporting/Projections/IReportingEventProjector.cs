using System;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Messaging;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Strategy interface for projecting incoming integration events into reporting read models (Open/Closed Principle).
/// Adding a new reporting projection only requires implementing this interface and registering it in DI.
/// </summary>
public interface IReportingEventProjector
{
    /// <summary>
    /// Event type name matching the IntegrationEventEnvelope.EventType (e.g. "ShipmentCreatedV1").
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Asynchronously projects the event into reporting read models.
    /// </summary>
    Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct);
}
