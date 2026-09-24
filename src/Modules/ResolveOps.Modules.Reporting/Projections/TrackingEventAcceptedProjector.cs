using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Projections;

/// <summary>
/// Projects TrackingEventAcceptedV1 events into carrier performance snapshots.
/// </summary>
public sealed class TrackingEventAcceptedProjector : IReportingEventProjector
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TimeProvider _timeProvider;

    public TrackingEventAcceptedProjector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string EventType => "TrackingEventAcceptedV1";

    public async Task ProjectAsync(AppDbContext dbContext, Guid tenantId, IntegrationEventEnvelope envelope, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TrackingEventAcceptedV1>(envelope.Payload, _jsonOptions);
        if (evt == null || evt.CarrierId == Guid.Empty) return;

        var periodDate = DateOnly.FromDateTime(envelope.OccurredAtUtc.UtcDateTime);
        var snapshot = await ReportingSnapshotHelper.GetOrCreateSnapshotAsync(dbContext, tenantId, evt.CarrierId, periodDate, _timeProvider, ct);

        var isDelayed = evt.NormalizedEventType.Contains("Delayed", StringComparison.OrdinalIgnoreCase);
        var isOnTime = evt.NormalizedEventType.Contains("Delivered", StringComparison.OrdinalIgnoreCase) && !isDelayed;

        snapshot.RecordTrackingEvent(isDelayed, isOnTime, _timeProvider);
    }
}
