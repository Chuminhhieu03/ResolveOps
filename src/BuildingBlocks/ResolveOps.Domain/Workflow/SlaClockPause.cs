namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Entity representing an interval during which an SLA clock was paused (spec §15.8).
/// </summary>
public sealed class SlaClockPause : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid SlaClockId { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public Guid? StartedBy { get; private set; }
    public Guid? EndedBy { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private SlaClockPause() { }

    public static SlaClockPause Create(
        Guid tenantId,
        Guid slaClockId,
        string reasonCode,
        DateTimeOffset startedAtUtc,
        Guid? startedBy,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new SlaClockPause
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SlaClockId = slaClockId,
            ReasonCode = reasonCode.Trim(),
            StartedAtUtc = startedAtUtc,
            StartedBy = startedBy,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void End(DateTimeOffset endedAtUtc, Guid? endedBy, TimeProvider timeProvider)
    {
        EndedAtUtc = endedAtUtc;
        EndedBy = endedBy;
        UpdatedAtUtc = timeProvider.GetUtcNow();
    }
}
