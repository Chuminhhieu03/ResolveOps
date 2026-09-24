using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Projects ClaimRecoveryRecordedV1 events into carrier performance snapshots.
/// </summary>
public sealed class ClaimRecoveryRecordedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public ClaimRecoveryRecordedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "ClaimRecoveryRecordedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimRecoveryRecordedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == evt.ClaimId, ct);
        if (claim == null || claim.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.ReceivedAtUtc.UtcDateTime);
        var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, claim.CarrierId, periodDate, _timeProvider, ct);
        snapshot.RecordClaimRecovery(evt.Amount, _timeProvider);
    }
}
