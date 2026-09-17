namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Exception case aggregate root (spec §15.7).
///
/// Invariants (spec §10.3):
/// 1. An active exception fingerprint is unique (enforced via partial unique index UIX_ExceptionCases_ActiveFingerprint).
/// 2. A closed case cannot be modified except through a reopen transition.
/// 3. Severity override requires reason and permission.
/// 4. Owner changes require audit.
/// 5. State transition must match the current version (ConcurrencyStamp check).
/// 6. A false-positive cancellation stores the reason/evidence and moves state to Cancelled.
/// 7. Financial exposure cannot be negative.
/// </summary>
public sealed class ExceptionCase : IAuditableEntity, IHasConcurrencyStamp
{
    private readonly List<ExceptionOccurrence> _occurrences = [];
    private readonly List<CaseTimelineEntry> _timelineEntries = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string CaseNumber { get; private set; } = string.Empty;
    public Guid ShipmentId { get; private set; }
    public Guid? ShipmentLegId { get; private set; }
    public string ExceptionType { get; private set; } = string.Empty;
    public string Fingerprint { get; private set; } = string.Empty;
    public string Status { get; private set; } = ExceptionCaseStatus.Detected;
    public string Severity { get; private set; } = ExceptionSeverity.Medium;
    public int? SeverityScore { get; private set; }

    public Guid PolicyId { get; private set; }
    public int PolicyVersionNumber { get; private set; }

    public Guid? OwnerUserId { get; private set; }
    public string? OwnerTeamCode { get; private set; }

    public decimal FinancialExposure { get; private set; }
    public string ExposureCurrency { get; private set; } = "USD";

    public string? RootCauseCode { get; private set; }
    public string? DispositionCode { get; private set; }

    public DateTimeOffset DetectedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp (ADR-006)
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public IReadOnlyList<ExceptionOccurrence> Occurrences => _occurrences.AsReadOnly();
    public IReadOnlyList<CaseTimelineEntry> TimelineEntries => _timelineEntries.AsReadOnly();

    private ExceptionCase() { }

    public static ExceptionCase Create(
        Guid tenantId,
        string caseNumber,
        Guid shipmentId,
        Guid? shipmentLegId,
        string exceptionType,
        string fingerprint,
        string severity,
        int? severityScore,
        Guid policyId,
        int policyVersionNumber,
        string? ownerTeamCode,
        decimal financialExposure,
        string exposureCurrency,
        DateTimeOffset detectedAtUtc,
        TimeProvider timeProvider,
        string? initialSummary = null,
        string? correlationId = null,
        Guid? actorId = null,
        string actorType = ActorType.System)
    {
        if (financialExposure < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(financialExposure), "Financial exposure cannot be negative (spec §10.3).");
        }

        var caseEntity = new ExceptionCase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseNumber = caseNumber,
            ShipmentId = shipmentId,
            ShipmentLegId = shipmentLegId,
            ExceptionType = exceptionType,
            Fingerprint = fingerprint,
            Status = ExceptionCaseStatus.Detected,
            Severity = severity,
            SeverityScore = severityScore,
            PolicyId = policyId,
            PolicyVersionNumber = policyVersionNumber,
            OwnerTeamCode = ownerTeamCode,
            FinancialExposure = financialExposure,
            ExposureCurrency = exposureCurrency.ToUpperInvariant(),
            DetectedAtUtc = detectedAtUtc,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        var timelineSummary = initialSummary ?? $"Exception detected ({exceptionType}) with severity {severity}.";
        caseEntity.AddTimelineEntry(
            CaseTimelineEntryType.Detected,
            actorType,
            actorId,
            timelineSummary,
            detailsJson: null,
            correlationId,
            timeProvider);

        return caseEntity;
    }

    public ExceptionOccurrence AddOccurrence(
        Guid? trackingEventId,
        string occurrenceType,
        DateTimeOffset observedAtUtc,
        string summary,
        TimeProvider timeProvider)
    {
        var occurrence = ExceptionOccurrence.Create(
            TenantId,
            Id,
            trackingEventId,
            occurrenceType,
            observedAtUtc,
            summary,
            timeProvider);

        _occurrences.Add(occurrence);

        AddTimelineEntry(
            CaseTimelineEntryType.OccurrenceAdded,
            ActorType.System,
            null,
            $"Additional occurrence observed: {summary}",
            detailsJson: null,
            correlationId: null,
            timeProvider);

        ConcurrencyStamp = Guid.NewGuid().ToString("N");
        return occurrence;
    }

    public CaseTimelineEntry AddTimelineEntry(
        string entryType,
        string actorType,
        Guid? actorId,
        string summary,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        var entry = CaseTimelineEntry.Create(
            TenantId,
            Id,
            entryType,
            actorType,
            actorId,
            summary,
            detailsJson,
            correlationId,
            timeProvider);

        _timelineEntries.Add(entry);
        return entry;
    }

    public Result Cancel(
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status == ExceptionCaseStatus.Cancelled)
        {
            return Result.Success();
        }

        if (!ExceptionCaseStatus.CanCancel(Status))
        {
            return DomainError.Failure(
                "ERR_CASE_CANNOT_CANCEL",
                $"Case '{CaseNumber}' in status '{Status}' cannot be cancelled as a false positive. Only cases in 'Detected' or 'Triaged' states may be cancelled.");
        }

        Status = ExceptionCaseStatus.Cancelled;
        ResolvedAtUtc = timeProvider.GetUtcNow();
        ClosedAtUtc = ResolvedAtUtc;

        AddTimelineEntry(
            CaseTimelineEntryType.Cancelled,
            actorType,
            actorId,
            $"Case cancelled: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        ConcurrencyStamp = Guid.NewGuid().ToString("N");
        return Result.Success();
    }
}
