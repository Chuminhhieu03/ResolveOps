using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Projects ShipmentCreatedV1 events into carrier performance snapshots.
/// Supports multi-leg shipments by attributing volume to all assigned carriers.
/// </summary>
public sealed class ShipmentCreatedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public ShipmentCreatedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "ShipmentCreatedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ShipmentCreatedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        // Query all carriers assigned across all legs of the shipment (supporting multi-leg shipments)
        var carrierIds = await dbContext.ShipmentLegs
            .Where(l => l.ShipmentId == evt.ShipmentId && l.CarrierId != Guid.Empty)
            .Select(l => l.CarrierId)
            .Distinct()
            .ToListAsync(ct);

        if (carrierIds.Count == 0) return;

        var periodDate = DateOnly.FromDateTime(envelope.OccurredAtUtc.UtcDateTime);

        foreach (var carrierId in carrierIds)
        {
            var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, carrierId, periodDate, _timeProvider, ct);
            snapshot.RecordShipment(_timeProvider);
        }
    }
}
