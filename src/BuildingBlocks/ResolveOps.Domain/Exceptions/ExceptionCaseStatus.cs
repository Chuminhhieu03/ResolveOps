namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Exception case lifecycle states (spec §9.1).
/// </summary>
public static class ExceptionCaseStatus
{
    public const string Detected = "Detected";
    public const string Triaged = "Triaged";
    public const string Assigned = "Assigned";
    public const string Investigating = "Investigating";
    public const string AwaitingEvidence = "AwaitingEvidence";
    public const string AwaitingCarrier = "AwaitingCarrier";
    public const string Mitigating = "Mitigating";
    public const string ClaimRequired = "ClaimRequired";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Reopened = "Reopened";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Detected,
        Triaged,
        Assigned,
        Investigating,
        AwaitingEvidence,
        AwaitingCarrier,
        Mitigating,
        ClaimRequired,
        Resolved,
        Closed,
        Reopened,
        Cancelled
    ];

    public static bool IsActive(string status) =>
        !string.Equals(status, Closed, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

    public static bool CanCancel(string status) =>
        string.Equals(status, Detected, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Triaged, StringComparison.OrdinalIgnoreCase);
}
