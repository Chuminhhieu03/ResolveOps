using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain.Reporting;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Shared helper for finding or creating carrier performance snapshots.
/// </summary>
public static class ReportingSnapshotHelper
{
    public static async Task<CarrierPerformanceSnapshot> GetOrCreateSnapshotAsync(
        AppDbContext dbContext,
        Guid tenantId,
        Guid carrierId,
        DateOnly periodDate,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var snapshot = await dbContext.CarrierPerformanceSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.CarrierId == carrierId && s.PeriodDate == periodDate, ct);

        if (snapshot != null)
        {
            return snapshot;
        }

        var carrier = await dbContext.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId, ct);
        var carrierName = carrier?.Name ?? "Carrier";

        snapshot = CarrierPerformanceSnapshot.Create(tenantId, carrierId, carrierName, periodDate, timeProvider);
        dbContext.CarrierPerformanceSnapshots.Add(snapshot);
        return snapshot;
    }
}
