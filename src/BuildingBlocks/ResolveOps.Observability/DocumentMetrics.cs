using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

/// <summary>
/// OpenTelemetry metrics and activity source for the document processing pipeline
/// (spec §20.3, §24 Phase 9).
/// </summary>
public static class DocumentMetrics
{
    public const string MeterName = "ResolveOps.Documents";
    public const string ActivitySourceName = "ResolveOps.Documents";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    /// <summary>Total upload intents created (presigned URL issued).</summary>
    public static readonly Counter<long> UploadedTotal = Meter.CreateCounter<long>(
        "documents.uploaded.total",
        unit: "{document}",
        description: "Total number of evidence document upload intents created");

    /// <summary>Total documents that completed scanning (clean + malicious + failed).</summary>
    public static readonly Counter<long> ScannedTotal = Meter.CreateCounter<long>(
        "documents.scanned.total",
        unit: "{document}",
        description: "Total number of evidence documents that completed the malware scan");

    /// <summary>Total documents quarantined due to malware detection.</summary>
    public static readonly Counter<long> QuarantinedTotal = Meter.CreateCounter<long>(
        "documents.quarantined.total",
        unit: "{document}",
        description: "Total number of evidence documents quarantined due to malware detection");

    /// <summary>Total documents rejected (scan failed after max retry).</summary>
    public static readonly Counter<long> RejectedTotal = Meter.CreateCounter<long>(
        "documents.rejected.total",
        unit: "{document}",
        description: "Total number of evidence documents rejected due to scan failure");

    /// <summary>Total abandoned upload intents cleaned up.</summary>
    public static readonly Counter<long> AbandonedCleanedTotal = Meter.CreateCounter<long>(
        "documents.abandoned.cleaned.total",
        unit: "{document}",
        description: "Total number of abandoned evidence upload intents marked Removed");

    /// <summary>Duration of the malware scan step in milliseconds.</summary>
    public static readonly Histogram<double> ScanDurationMs = Meter.CreateHistogram<double>(
        "documents.scan.duration.ms",
        unit: "ms",
        description: "Duration of the malware scan step in milliseconds");
}
