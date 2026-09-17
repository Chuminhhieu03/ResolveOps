namespace ResolveOps.Domain.Workflow;

/// <summary>
/// SLA clock aggregate root (spec §9.4, §15.8).
///
/// State machine:
/// NotStarted -> Running -> Completed
/// Running -> Paused -> Running
/// Running -> Breached
/// </summary>
public sealed class SlaClock : IAuditableEntity, IHasConcurrencyStamp
{
    private readonly List<SlaClockPause> _pauses = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid? ClaimId { get; private set; }
    public string ClockType { get; private set; } = string.Empty;
    public Guid PolicyVersionId { get; private set; }
    public string Status { get; private set; } = SlaClockStatus.NotStarted;

    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? DueAtUtc { get; private set; }
    public DateTimeOffset? PausedAtUtc { get; private set; }
    public long TotalPausedSeconds { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? BreachedAtUtc { get; private set; }

    public IReadOnlyList<SlaClockPause> Pauses => _pauses.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private SlaClock() { }

    public static SlaClock Create(
        Guid tenantId,
        Guid caseId,
        string clockType,
        Guid policyVersionId,
        TimeProvider timeProvider,
        Guid? claimId = null)
    {
        var now = timeProvider.GetUtcNow();
        return new SlaClock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            ClaimId = claimId,
            ClockType = clockType,
            PolicyVersionId = policyVersionId,
            Status = SlaClockStatus.NotStarted,
            TotalPausedSeconds = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public Result Start(DateTimeOffset dueAtUtc, TimeProvider timeProvider)
    {
        if (Status != SlaClockStatus.NotStarted)
        {
            return DomainError.Failure(
                "ERR_CLOCK_CANNOT_START",
                $"SLA clock '{ClockType}' for case '{CaseId}' cannot be started from status '{Status}'.");
        }

        var now = timeProvider.GetUtcNow();
        Status = SlaClockStatus.Running;
        StartedAtUtc = now;
        DueAtUtc = dueAtUtc;

        return Result.Success();
    }

    public Result Pause(string reasonCode, Guid? actorId, TimeProvider timeProvider)
    {
        if (Status != SlaClockStatus.Running)
        {
            return DomainError.Failure(
                "ERR_CLOCK_CANNOT_PAUSE",
                $"SLA clock '{ClockType}' for case '{CaseId}' cannot be paused from status '{Status}'.");
        }

        var now = timeProvider.GetUtcNow();
        Status = SlaClockStatus.Paused;
        PausedAtUtc = now;

        var pause = SlaClockPause.Create(TenantId, Id, reasonCode, now, actorId, timeProvider);
        _pauses.Add(pause);

        return Result.Success();
    }

    public Result Resume(DateTimeOffset newDueAtUtc, Guid? actorId, TimeProvider timeProvider)
    {
        if (Status != SlaClockStatus.Paused)
        {
            return DomainError.Failure(
                "ERR_CLOCK_CANNOT_RESUME",
                $"SLA clock '{ClockType}' for case '{CaseId}' cannot be resumed from status '{Status}'.");
        }

        var now = timeProvider.GetUtcNow();
        if (PausedAtUtc.HasValue)
        {
            var pausedSeconds = (long)(now - PausedAtUtc.Value).TotalSeconds;
            TotalPausedSeconds += Math.Max(0, pausedSeconds);
        }

        var activePause = _pauses.LastOrDefault(p => p.EndedAtUtc == null);
        activePause?.End(now, actorId, timeProvider);

        Status = SlaClockStatus.Running;
        PausedAtUtc = null;
        DueAtUtc = newDueAtUtc;

        return Result.Success();
    }

    public Result Complete(TimeProvider timeProvider)
    {
        if (Status == SlaClockStatus.Completed)
        {
            return Result.Success();
        }

        if (Status != SlaClockStatus.Running && Status != SlaClockStatus.Paused)
        {
            return DomainError.Failure(
                "ERR_CLOCK_CANNOT_COMPLETE",
                $"SLA clock '{ClockType}' for case '{CaseId}' cannot be completed from status '{Status}'.");
        }

        var now = timeProvider.GetUtcNow();
        if (Status == SlaClockStatus.Paused)
        {
            var activePause = _pauses.LastOrDefault(p => p.EndedAtUtc == null);
            activePause?.End(now, null, timeProvider);
        }

        Status = SlaClockStatus.Completed;
        CompletedAtUtc = now;

        return Result.Success();
    }

    public Result Breach(TimeProvider timeProvider)
    {
        if (Status != SlaClockStatus.Running)
        {
            return DomainError.Failure(
                "ERR_CLOCK_CANNOT_BREACH",
                $"SLA clock '{ClockType}' for case '{CaseId}' cannot be breached from status '{Status}'.");
        }

        var now = timeProvider.GetUtcNow();
        Status = SlaClockStatus.Breached;
        BreachedAtUtc = now;

        return Result.Success();
    }
}
