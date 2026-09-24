using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Projects ClaimSubmittedV1 events into carrier performance snapshots.
/// </summary>
public sealed class ClaimSubmittedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public ClaimSubmittedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "ClaimSubmittedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ClaimSubmittedV1>(envelope.Payload, _jsonOptions);
        if (evt == null || evt.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(evt.SubmittedAtUtc.UtcDateTime);
        var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, evt.CarrierId, periodDate, _timeProvider, ct);
        snapshot.RecordClaimSubmitted(evt.ClaimedAmount, _timeProvider);
    }
}
