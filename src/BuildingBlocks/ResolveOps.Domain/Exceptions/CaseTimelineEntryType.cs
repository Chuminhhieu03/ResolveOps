namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Audit trail entry types for case timeline events (spec §15.7).
/// </summary>
public static class CaseTimelineEntryType
{
    public const string Detected = "Detected";
    public const string OccurrenceAdded = "OccurrenceAdded";
    public const string Cancelled = "Cancelled";
    public const string ManualCreated = "ManualCreated";
    public const string SeverityChanged = "SeverityChanged";
    public const string Reclassified = "Reclassified";
    public const string Triaged = "Triaged";
    public const string Assigned = "Assigned";
    public const string InvestigationStarted = "InvestigationStarted";
    public const string AwaitingEvidence = "AwaitingEvidence";
    public const string EvidenceReceived = "EvidenceReceived";
    public const string AwaitingCarrier = "AwaitingCarrier";
    public const string CarrierUpdated = "CarrierUpdated";
    public const string MitigationStarted = "MitigationStarted";
    public const string MitigationCompleted = "MitigationCompleted";
    public const string ClaimRequired = "ClaimRequired";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Reopened = "Reopened";
    public const string CommentAdded = "CommentAdded";
    public const string SlaBreached = "SlaBreached";
    public const string SlaPaused = "SlaPaused";
    public const string SlaResumed = "SlaResumed";
    public const string SlaCompleted = "SlaCompleted";
}
