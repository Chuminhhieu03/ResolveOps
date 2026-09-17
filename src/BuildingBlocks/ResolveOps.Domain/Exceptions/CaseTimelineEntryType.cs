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
    public const string CommentAdded = "CommentAdded";
}
