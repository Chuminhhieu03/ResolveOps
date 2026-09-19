namespace ResolveOps.Domain.Documents;

/// <summary>
/// Malware scan status constants for EvidenceDocument (spec §8.6, §10.4).
///
/// A document is unavailable to business workflows (evidence checklists, claim readiness)
/// until ScanStatus is <see cref="Clean"/> and Status is <see cref="DocumentStatus.Available"/>.
/// </summary>
public static class DocumentScanStatus
{
    /// <summary>Scan not yet initiated; document is PendingUpload or just transitioned to PendingScan.</summary>
    public const string Pending = "Pending";

    /// <summary>No threat detected; document transitions to Available.</summary>
    public const string Clean = "Clean";

    /// <summary>Malware or suspicious content detected; document transitions to Quarantined.</summary>
    public const string Malicious = "Malicious";

    /// <summary>Scanner returned an error; document transitions to Rejected after max retry.</summary>
    public const string Failed = "Failed";
}
