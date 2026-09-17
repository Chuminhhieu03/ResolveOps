namespace ResolveOps.Domain.Workflow;

/// <summary>
/// SLA policy version entity (spec §15.8).
/// Defines specific target durations (in minutes) and business calendar linkage.
/// </summary>
public sealed class SlaPolicyVersion : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid SlaPolicyId { get; private set; }
    public int VersionNumber { get; private set; }
    public Guid? CalendarId { get; private set; }
    public int? AcknowledgementMinutes { get; private set; }
    public int? FirstActionMinutes { get; private set; }
    public int? ResolutionMinutes { get; private set; }
    public int? ClaimSubmissionMinutes { get; private set; }
    public string? PauseReasonCodes { get; private set; }
    public DateTimeOffset EffectiveFromUtc { get; private set; }
    public DateTimeOffset? EffectiveToUtc { get; private set; }
    public string Status { get; private set; } = "Active";

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private SlaPolicyVersion() { }

    public static SlaPolicyVersion Create(
        Guid tenantId,
        Guid slaPolicyId,
        int versionNumber,
        Guid? calendarId,
        int? acknowledgementMinutes,
        int? firstActionMinutes,
        int? resolutionMinutes,
        int? claimSubmissionMinutes,
        string? pauseReasonCodes,
        DateTimeOffset effectiveFromUtc,
        TimeProvider timeProvider,
        string status = "Active")
    {
        var now = timeProvider.GetUtcNow();
        return new SlaPolicyVersion
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SlaPolicyId = slaPolicyId,
            VersionNumber = versionNumber,
            CalendarId = calendarId,
            AcknowledgementMinutes = acknowledgementMinutes,
            FirstActionMinutes = firstActionMinutes,
            ResolutionMinutes = resolutionMinutes,
            ClaimSubmissionMinutes = claimSubmissionMinutes,
            PauseReasonCodes = pauseReasonCodes,
            EffectiveFromUtc = effectiveFromUtc,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }
}
