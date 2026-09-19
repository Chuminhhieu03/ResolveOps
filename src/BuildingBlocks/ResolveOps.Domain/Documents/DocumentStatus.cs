namespace ResolveOps.Domain.Documents;

/// <summary>
/// Document status constants for EvidenceDocument (spec §8.6, §10.4, §15.9).
///
/// State machine:
///   PendingUpload → PendingScan (client calls complete-upload)
///   PendingScan   → Available   (scan: clean)
///   PendingScan   → Quarantined (scan: malicious)
///   PendingScan   → Rejected    (scan: failed after max retries, or unsupported content)
///   Available     → Superseded  (newer version uploaded)
///   Available     → Removed     (user removes; LegalHold must be false)
///   PendingUpload → Removed     (abandoned upload cleanup job)
/// </summary>
public static class DocumentStatus
{
    /// <summary>Upload intent created; presigned URL issued; object not yet uploaded.</summary>
    public const string PendingUpload = "PendingUpload";

    /// <summary>Client confirmed upload; object awaiting scan worker.</summary>
    public const string PendingScan = "PendingScan";

    /// <summary>Scan completed clean; document available for business workflows.</summary>
    public const string Available = "Available";

    /// <summary>A newer version has been uploaded; this version is retained for audit.</summary>
    public const string Superseded = "Superseded";

    /// <summary>Content failed validation (unsupported, corrupt, or scan error).</summary>
    public const string Rejected = "Rejected";

    /// <summary>Malware detected; document isolated. Cannot satisfy evidence checklists.</summary>
    public const string Quarantined = "Quarantined";

    /// <summary>Soft-removed by user or cleanup job. LegalHold must be false to remove.</summary>
    public const string Removed = "Removed";

    private static readonly HashSet<string> _terminal =
    [
        Quarantined, Removed,
    ];

    /// <summary>Returns true for states that are permanently non-actionable.</summary>
    public static bool IsTerminal(string status) => _terminal.Contains(status);
}
