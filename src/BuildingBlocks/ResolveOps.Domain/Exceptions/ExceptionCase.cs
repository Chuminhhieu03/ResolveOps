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
/// 6. A case cannot close with mandatory incomplete tasks unless each task is waived by an authorized actor with reason.
/// 7. A false-positive cancellation stores the reason/evidence and moves state to Cancelled.
/// 8. Financial exposure cannot be negative.
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

    public Result Triage(
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Detected)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot transition to '{ExceptionCaseStatus.Triaged}' from status '{Status}'. Only 'Detected' cases can be triaged.");
        }

        Status = ExceptionCaseStatus.Triaged;
        AddTimelineEntry(
            CaseTimelineEntryType.Triaged,
            actorType,
            actorId,
            "Case triaged by coordinator.",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result Assign(
        Guid? ownerUserId,
        string? ownerTeamCode,
        string? reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (!ExceptionCaseStatus.IsActive(Status))
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is in inactive status '{Status}' and cannot be assigned.");
        }

        var oldOwner = OwnerUserId;
        var oldTeam = OwnerTeamCode;

        OwnerUserId = ownerUserId;
        OwnerTeamCode = ownerTeamCode;

        if (Status == ExceptionCaseStatus.Triaged)
        {
            Status = ExceptionCaseStatus.Assigned;
        }

        var summary = $"Case assigned to user '{ownerUserId}' / team '{ownerTeamCode}'. Previous: user '{oldOwner}' / team '{oldTeam}'. Reason: {reason}";
        AddTimelineEntry(
            CaseTimelineEntryType.Assigned,
            actorType,
            actorId,
            summary,
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result StartInvestigation(
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Assigned && Status != ExceptionCaseStatus.Reopened)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot start investigation from status '{Status}'. Must be in 'Assigned' or 'Reopened' status.");
        }

        Status = ExceptionCaseStatus.Investigating;
        AddTimelineEntry(
            CaseTimelineEntryType.InvestigationStarted,
            actorType,
            actorId,
            "Investigation started.",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result RequestEvidence(
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Investigating)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot transition to '{ExceptionCaseStatus.AwaitingEvidence}' from status '{Status}'.");
        }

        Status = ExceptionCaseStatus.AwaitingEvidence;
        AddTimelineEntry(
            CaseTimelineEntryType.AwaitingEvidence,
            actorType,
            actorId,
            $"Evidence requested: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result ReceiveEvidence(
        string? notes,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.AwaitingEvidence)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is not in '{ExceptionCaseStatus.AwaitingEvidence}' status.");
        }

        Status = ExceptionCaseStatus.Investigating;
        AddTimelineEntry(
            CaseTimelineEntryType.EvidenceReceived,
            actorType,
            actorId,
            $"Evidence received. Investigation resumed. Note: {notes}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result RecordCarrierUpdate(
        string notes,
        string? targetStatus,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (!ExceptionCaseStatus.IsActive(Status))
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is not active.");
        }

        if (string.Equals(targetStatus, ExceptionCaseStatus.AwaitingCarrier, StringComparison.OrdinalIgnoreCase))
        {
            if (Status != ExceptionCaseStatus.Investigating)
            {
                return DomainError.Failure(
                    "ERR_INVALID_STATE_TRANSITION",
                    $"Case '{CaseNumber}' must be in 'Investigating' status to wait for carrier update.");
            }
            Status = ExceptionCaseStatus.AwaitingCarrier;
            AddTimelineEntry(
                CaseTimelineEntryType.AwaitingCarrier,
                actorType,
                actorId,
                $"Awaiting carrier update: {notes}",
                detailsJson,
                correlationId,
                timeProvider);
        }
        else if (Status == ExceptionCaseStatus.AwaitingCarrier)
        {
            Status = ExceptionCaseStatus.Investigating;
            AddTimelineEntry(
                CaseTimelineEntryType.CarrierUpdated,
                actorType,
                actorId,
                $"Carrier update received. Investigation resumed. Note: {notes}",
                detailsJson,
                correlationId,
                timeProvider);
        }
        else
        {
            AddTimelineEntry(
                CaseTimelineEntryType.CarrierUpdated,
                actorType,
                actorId,
                $"Carrier update recorded: {notes}",
                detailsJson,
                correlationId,
                timeProvider);
        }

        return Result.Success();
    }

    public Result StartMitigation(
        string? plan,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Investigating)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' must be in 'Investigating' status to start mitigation.");
        }

        Status = ExceptionCaseStatus.Mitigating;
        AddTimelineEntry(
            CaseTimelineEntryType.MitigationStarted,
            actorType,
            actorId,
            $"Mitigation started: {plan}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result CompleteMitigation(
        string? outcome,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Mitigating)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is not in 'Mitigating' status.");
        }

        Status = ExceptionCaseStatus.Investigating;
        AddTimelineEntry(
            CaseTimelineEntryType.MitigationCompleted,
            actorType,
            actorId,
            $"Mitigation completed: {outcome}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result MarkClaimRequired(
        string claimType,
        decimal? estimatedLoss,
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Investigating)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' must be in 'Investigating' status to mark claim required.");
        }

        Status = ExceptionCaseStatus.ClaimRequired;
        if (estimatedLoss.HasValue && estimatedLoss.Value >= 0)
        {
            FinancialExposure = estimatedLoss.Value;
        }

        AddTimelineEntry(
            CaseTimelineEntryType.ClaimRequired,
            actorType,
            actorId,
            $"Confirmed financial claim required ({claimType}). Reason: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result Resolve(
        string? resolutionCode,
        string? rootCauseCode,
        string? dispositionCode,
        string notes,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Investigating && Status != ExceptionCaseStatus.ClaimRequired)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot be resolved from status '{Status}'. Must be in 'Investigating' or 'ClaimRequired'.");
        }

        Status = ExceptionCaseStatus.Resolved;
        ResolvedAtUtc = timeProvider.GetUtcNow();
        RootCauseCode = rootCauseCode?.Trim() ?? RootCauseCode;
        DispositionCode = dispositionCode?.Trim() ?? DispositionCode;

        AddTimelineEntry(
            CaseTimelineEntryType.Resolved,
            actorType,
            actorId,
            $"Case resolved. Code: {resolutionCode}. Notes: {notes}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result Close(
        string? notes,
        bool hasIncompleteMandatoryTasks,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Resolved)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot be closed from status '{Status}'. Only 'Resolved' cases can be closed.");
        }

        // Spec §10.3 Invariant 6: A case cannot close with mandatory incomplete tasks unless waived by an authorized actor with reason
        if (hasIncompleteMandatoryTasks)
        {
            return DomainError.Failure(
                "ERR_CASE_MANDATORY_TASKS_INCOMPLETE",
                $"Case '{CaseNumber}' cannot be closed because it has incomplete mandatory tasks that have not been waived.");
        }

        Status = ExceptionCaseStatus.Closed;
        ClosedAtUtc = timeProvider.GetUtcNow();

        AddTimelineEntry(
            CaseTimelineEntryType.Closed,
            actorType,
            actorId,
            $"Case closed. Note: {notes}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result Reopen(
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (Status != ExceptionCaseStatus.Closed)
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' cannot be reopened from status '{Status}'. Only 'Closed' cases can be reopened.");
        }

        Status = ExceptionCaseStatus.Reopened;
        ResolvedAtUtc = null;
        ClosedAtUtc = null;

        AddTimelineEntry(
            CaseTimelineEntryType.Reopened,
            actorType,
            actorId,
            $"Case reopened: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result ChangeSeverity(
        string newSeverity,
        string? reasonCode,
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (!ExceptionCaseStatus.IsActive(Status))
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is inactive ({Status}) and severity cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "A reason is required to change case severity (spec §10.3).");
        }

        var oldSeverity = Severity;
        Severity = newSeverity;

        AddTimelineEntry(
            CaseTimelineEntryType.SeverityChanged,
            actorType,
            actorId,
            $"Severity changed from '{oldSeverity}' to '{newSeverity}'. Reason code: '{reasonCode}'. Reason: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result Reclassify(
        string newExceptionType,
        string reason,
        Guid? actorId,
        string actorType,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (!ExceptionCaseStatus.IsActive(Status))
        {
            return DomainError.Failure(
                "ERR_INVALID_STATE_TRANSITION",
                $"Case '{CaseNumber}' is inactive ({Status}) and cannot be reclassified.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "A reason is required to reclassify a case (spec §8.4).");
        }

        var oldType = ExceptionType;
        ExceptionType = newExceptionType;

        AddTimelineEntry(
            CaseTimelineEntryType.Reclassified,
            actorType,
            actorId,
            $"Case reclassified from '{oldType}' to '{newExceptionType}'. Reason: {reason}",
            detailsJson,
            correlationId,
            timeProvider);

        return Result.Success();
    }

    public Result AddComment(
        string comment,
        Guid? actorId,
        string actorType,
        string? correlationId,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "Comment cannot be empty.");
        }

        AddTimelineEntry(
            CaseTimelineEntryType.CommentAdded,
            actorType,
            actorId,
            comment.Trim(),
            detailsJson: null,
            correlationId,
            timeProvider);

        return Result.Success();
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

        return Result.Success();
    }
}
