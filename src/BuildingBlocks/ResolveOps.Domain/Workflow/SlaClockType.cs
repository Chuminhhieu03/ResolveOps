namespace ResolveOps.Domain.Workflow;

/// <summary>
/// SLA clock types supported by the system (spec §9.4, §15.8).
/// </summary>
public static class SlaClockType
{
    public const string FirstAction = "FirstAction";
    public const string Resolution = "Resolution";
    public const string Acknowledgement = "Acknowledgement";
    public const string ClaimSubmission = "ClaimSubmission";

    public static readonly IReadOnlyList<string> All =
    [
        FirstAction,
        Resolution,
        Acknowledgement,
        ClaimSubmission
    ];
}
