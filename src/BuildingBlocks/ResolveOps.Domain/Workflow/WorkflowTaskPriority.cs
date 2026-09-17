namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Operational priority levels for workflow tasks.
/// </summary>
public static class WorkflowTaskPriority
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly IReadOnlyList<string> All =
    [
        Low,
        Normal,
        High,
        Urgent
    ];
}
