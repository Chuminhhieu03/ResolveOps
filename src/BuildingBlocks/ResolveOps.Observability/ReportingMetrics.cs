using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

public static class ReportingMetrics
{
    public const string MeterName = "ResolveOps.Reporting";
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly ActivitySource ActivitySource = new(MeterName);

    public static readonly Histogram<double> QueriesDurationSeconds = Meter.CreateHistogram<double>(
        "reporting.queries.duration.seconds",
        unit: "s",
        description: "Duration of analytical reporting queries in seconds");

    public static readonly Counter<long> ExportsTotal = Meter.CreateCounter<long>(
        "reporting.exports.total",
        description: "Total number of export requests initiated or completed");

    public static readonly Histogram<double> ExportsDurationSeconds = Meter.CreateHistogram<double>(
        "reporting.exports.duration.seconds",
        unit: "s",
        description: "Duration of asynchronous export processing in seconds");

    public static readonly Counter<long> ProjectionsProcessedTotal = Meter.CreateCounter<long>(
        "reporting.projections.processed.total",
        description: "Total number of events processed by reporting read-model projections");
}
