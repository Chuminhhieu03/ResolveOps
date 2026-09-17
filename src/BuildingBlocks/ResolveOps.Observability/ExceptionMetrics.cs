using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

/// <summary>
/// OpenTelemetry metrics and activity source for exception policy evaluation and case creation (spec §20.3, §20.4, §24 Phase 7).
/// </summary>
public static class ExceptionMetrics
{
    public const string MeterName = "ResolveOps.Exceptions";
    public const string ActivitySourceName = "ResolveOps.Exceptions";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    // Counters
    public static readonly Counter<long> ExceptionsDetectedTotal = Meter.CreateCounter<long>(
        "exceptions.detected.total",
        unit: "{exception}",
        description: "Total number of exceptions detected and cases created");

    // Duration histogram
    public static readonly Histogram<double> DetectionDurationMs = Meter.CreateHistogram<double>(
        "exceptions.detection.duration.ms",
        unit: "ms",
        description: "Duration of exception rule evaluation in milliseconds");
}
