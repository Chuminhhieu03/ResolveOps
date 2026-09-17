namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Task lifecycle states (spec §9.3).
/// </summary>
public static class WorkflowTaskStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Blocked = "Blocked";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Open,
        InProgress,
        Completed,
        Blocked,
        Cancelled
    ];

    public static bool IsTerminal(string status) =>
        string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);
}
