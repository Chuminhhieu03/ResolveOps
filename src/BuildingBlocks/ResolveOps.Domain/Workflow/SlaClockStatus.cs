namespace ResolveOps.Domain.Workflow;

/// <summary>
/// SLA clock states (spec §9.4).
/// </summary>
public static class SlaClockStatus
{
    public const string NotStarted = "NotStarted";
    public const string Running = "Running";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Breached = "Breached";

    public static readonly IReadOnlyList<string> All =
    [
        NotStarted,
        Running,
        Paused,
        Completed,
        Breached
    ];

    public static bool IsTerminal(string status) =>
        string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Breached, StringComparison.OrdinalIgnoreCase);
}
