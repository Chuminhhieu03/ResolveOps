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
/// Projects ClaimDecisionRecordedV1 events into carrier performance snapshots.
/// </summary>
public sealed class ClaimDecisionRecordedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public ClaimDecisionRecordedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "ClaimDecisionRecordedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimDecisionRecordedV1>(envelope.Payload, _jsonOptions);
        if (evt == null) return;

        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == evt.ClaimId, ct);
        if (claim == null || claim.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.RecordedAtUtc.UtcDateTime);
        var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, claim.CarrierId, periodDate, _timeProvider, ct);

        double? responseTime = claim.SubmittedAtUtc.HasValue
            ? (evt.RecordedAtUtc - claim.SubmittedAtUtc.Value).TotalHours
            : null;

        snapshot.RecordClaimDecision(evt.Decision, evt.ApprovedAmount, responseTime, _timeProvider);
    }
}
