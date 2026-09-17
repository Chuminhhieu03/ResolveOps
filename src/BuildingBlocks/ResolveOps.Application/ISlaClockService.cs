namespace ResolveOps.Application;

/// <summary>
/// Domain service coordinating SLA clock lifecycle transitions across exception workflows (spec §9.4, §15.8).
/// </summary>
public interface ISlaClockService
{
    /// <summary>
    /// Starts initial SLA clocks (FirstAction, Resolution) for a newly detected or created case.
    /// </summary>
    Task StartCaseClocksAsync(
        Guid tenantId,
        Guid caseId,
        Guid? slaPolicyVersionId,
        DateTimeOffset detectedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the specified SLA clock type for a case (e.g. FirstAction upon triage, Resolution upon resolve).
    /// </summary>
    Task CompleteClockAsync(
        Guid tenantId,
        Guid caseId,
        string clockType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses running SLA clocks for a case (e.g. when entering AwaitingEvidence or AwaitingCarrier).
    /// </summary>
    Task PauseClocksAsync(
        Guid tenantId,
        Guid caseId,
        string reasonCode,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes paused SLA clocks for a case and recalculates due dates using the business calendar.
    /// </summary>
    Task ResumeClocksAsync(
        Guid tenantId,
        Guid caseId,
        Guid? actorId,
        CancellationToken cancellationToken = default);
}
