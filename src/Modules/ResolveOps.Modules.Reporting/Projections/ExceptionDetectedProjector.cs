using System;
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
/// Projects ExceptionDetectedV1 events into carrier performance snapshots.
/// Accurately resolves the responsible carrier by checking the exception case's ShipmentLegId
/// rather than assuming SequenceNumber == 1.
/// </summary>
public sealed class ExceptionDetectedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public ExceptionDetectedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "ExceptionDetectedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ExceptionDetectedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        // 1. Look up the exception case to inspect the exact ShipmentLegId
        var exceptionCase = await dbContext.ExceptionCases
            .FirstOrDefaultAsync(c => c.Id == evt.CaseId, ct);

        Guid responsibleCarrierId = Guid.Empty;

        if (exceptionCase?.ShipmentLegId.HasValue == true && exceptionCase.ShipmentLegId.Value != Guid.Empty)
        {
            var leg = await dbContext.ShipmentLegs
                .FirstOrDefaultAsync(l => l.Id == exceptionCase.ShipmentLegId.Value, ct);

            if (leg != null && leg.CarrierId != Guid.Empty)
            {
                responsibleCarrierId = leg.CarrierId;
            }
        }

        // 2. If no specific leg was associated, fallback to first available leg or shipment carrier
        if (responsibleCarrierId == Guid.Empty)
        {
            var fallbackLeg = await dbContext.ShipmentLegs
                .Where(l => l.ShipmentId == evt.ShipmentId && l.CarrierId != Guid.Empty)
                .OrderBy(l => l.SequenceNumber)
                .FirstOrDefaultAsync(ct);

            if (fallbackLeg != null)
            {
                responsibleCarrierId = fallbackLeg.CarrierId;
            }
        }

        if (responsibleCarrierId != Guid.Empty)
        {
            var periodDate = DateOnly.FromDateTime(evt.DetectedAtUtc.UtcDateTime);
            var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, responsibleCarrierId, periodDate, _timeProvider, ct);
            snapshot.RecordException(evt.Severity, _timeProvider);
        }
    }
}
