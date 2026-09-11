using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

/// <summary>
/// OpenTelemetry metrics and activity source for tracking ingestion and normalization (spec §20.3, §20.4, §24 Phase 6).
/// </summary>
public static class TrackingMetrics
{
    public const string MeterName = "ResolveOps.Tracking";
    public const string ActivitySourceName = "ResolveOps.Tracking";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    // Counters
    public static readonly Counter<long> ReceiptsTotal = Meter.CreateCounter<long>(
        "tracking.receipts.total",
        unit: "{receipt}",
        description: "Total number of tracking receipts ingested");

    public static readonly Counter<long> NormalizedTotal = Meter.CreateCounter<long>(
        "tracking.normalized.total",
        unit: "{event}",
        description: "Total number of tracking events successfully normalized");

    public static readonly Counter<long> QuarantinedTotal = Meter.CreateCounter<long>(
        "tracking.quarantined.total",
        unit: "{event}",
        description: "Total number of tracking events quarantined");

    // Duration histogram
    public static readonly Histogram<double> NormalizationDurationMs = Meter.CreateHistogram<double>(
        "tracking.normalization.duration.ms",
        unit: "ms",
        description: "Duration of the tracking normalization process in milliseconds");
}
