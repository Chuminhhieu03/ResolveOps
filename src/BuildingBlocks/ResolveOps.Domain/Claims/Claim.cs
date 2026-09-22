using System;
using System.Collections.Generic;
using System.Linq;

using ResolveOps.Domain.Claims.ValueObjects;

namespace ResolveOps.Domain.Claims;

public sealed class Claim : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ClaimNumber { get; private set; } = string.Empty;
    public Guid CaseId { get; private set; }
    public Guid CarrierId { get; private set; }
    public string ClaimType { get; private set; } = string.Empty;
    public string Status { get; private set; } = ClaimStatus.Draft;

    public string EligibilityStatus { get; private set; } = ClaimEligibilityStatus.Pending;
    public string[] EligibilityReasonCodes { get; private set; } = [];
    public Guid? PolicyVersionId { get; private set; }
    public DateTimeOffset? ClaimDeadlineAtUtc { get; private set; }

    public decimal ClaimedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal RecoveredAmount { get; private set; }
    public decimal WrittenOffAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public string? ExternalSubmissionReference { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedForSubmissionBy { get; private set; }
    public DateTimeOffset? ApprovedForSubmissionAtUtc { get; private set; }
    public DateTimeOffset? WrittenOffAtUtc { get; private set; }
    public Guid? WrittenOffBy { get; private set; }
    public string? WriteOffReason { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public string? ClosingNotes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;

    private readonly List<ClaimLossComponent> _lossComponents = [];
    public IReadOnlyCollection<ClaimLossComponent> LossComponents => _lossComponents.AsReadOnly();

    private readonly List<ClaimApproval> _approvals = [];
    public IReadOnlyCollection<ClaimApproval> Approvals => _approvals.AsReadOnly();

    private readonly List<CarrierClaimResponse> _responses = [];
    public IReadOnlyCollection<CarrierClaimResponse> Responses => _responses.AsReadOnly();

    private readonly List<RecoveryTransaction> _recoveryTransactions = [];
    public IReadOnlyCollection<RecoveryTransaction> RecoveryTransactions => _recoveryTransactions.AsReadOnly();

    private Claim() { } // EF Core

    public static Result<Claim> CreateDraft(
        Guid id,
        Guid tenantId,
        string claimNumber,
        Guid caseId,
        Guid carrierId,
        string claimType,
        string currency,
        string eligibilityStatus,
        string[] eligibilityReasonCodes,
        Guid? policyVersionId,
        DateTimeOffset? deadlineAtUtc)
    {
        if (!ResolveOps.Domain.Claims.ClaimType.All.Contains(claimType))
            return Result<Claim>.Failure(new DomainError("INVALID_CLAIM_TYPE", "Invalid claim type."));

        if (!ResolveOps.Domain.Claims.ClaimEligibilityStatus.All.Contains(eligibilityStatus))
            return Result<Claim>.Failure(new DomainError("INVALID_ELIGIBILITY_STATUS", "Invalid eligibility status."));

        var claim = new Claim
        {
            Id = id,
            TenantId = tenantId,
            ClaimNumber = claimNumber,
            CaseId = caseId,
            CarrierId = carrierId,
            ClaimType = claimType,
            Currency = currency,
            Status = ClaimStatus.Draft,
            EligibilityStatus = eligibilityStatus,
            EligibilityReasonCodes = eligibilityReasonCodes,
            PolicyVersionId = policyVersionId,
            ClaimDeadlineAtUtc = deadlineAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return Result<Claim>.Success(claim);
    }

    public Result<ClaimLossComponent> AddLossComponent(
        string componentType,
        string description,
        decimal? quantity,
        decimal? unitAmount,
        Money amount,
        Guid? sourceDocumentId)
    {
        if (Status != ClaimStatus.Draft && Status != ClaimStatus.EvidencePending && Status != ClaimStatus.ReadyForReview)
        {
            return Result<ClaimLossComponent>.Failure(new DomainError("INVALID_STATE_TRANSITION", "Cannot modify loss components outside of draft or review states."));
        }

        if (!ResolveOps.Domain.Claims.LossComponentType.All.Contains(componentType))
            return Result<ClaimLossComponent>.Failure(new DomainError("INVALID_COMPONENT_TYPE", "Invalid loss component type."));

        if (amount.Currency != Currency)
        {
            return Result<ClaimLossComponent>.Failure(new DomainError("VALIDATION_FAILED", "Loss component currency must match claim currency."));
        }

        if (amount.Amount <= 0)
        {
            return Result<ClaimLossComponent>.Failure(new DomainError("VALIDATION_FAILED", "Loss component amount must be positive."));
        }

        var component = new ClaimLossComponent(
            Guid.NewGuid(),
            Id,
            componentType,
            description,
            quantity,
            unitAmount,
            amount,
            sourceDocumentId);

        _lossComponents.Add(component);
        RecalculateClaimedAmount();

        return Result<ClaimLossComponent>.Success(component);
    }

    public Result UpdateLossComponent(
        Guid componentId,
        string description,
        decimal? quantity,
        decimal? unitAmount,
        Money amount,
        Guid? sourceDocumentId)
    {
        if (Status != ClaimStatus.Draft && Status != ClaimStatus.EvidencePending && Status != ClaimStatus.ReadyForReview)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", "Cannot modify loss components outside of draft or review states."));
        }

        if (amount.Currency != Currency)
        {
            return Result.Failure(new DomainError("VALIDATION_FAILED", "Loss component currency must match claim currency."));
        }

        if (amount.Amount <= 0)
        {
            return Result.Failure(new DomainError("VALIDATION_FAILED", "Loss component amount must be positive."));
        }

        var component = _lossComponents.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
        {
            return Result.Failure(new DomainError("RESOURCE_NOT_FOUND", "Loss component not found."));
        }
        component.Update(description, quantity, unitAmount, amount, sourceDocumentId);
        RecalculateClaimedAmount();

        return Result.Success();
    }

    public Result RemoveLossComponent(Guid componentId)
    {
        if (Status != ClaimStatus.Draft && Status != ClaimStatus.EvidencePending && Status != ClaimStatus.ReadyForReview)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", "Cannot modify loss components outside of draft or review states."));
        }

        var component = _lossComponents.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
        {
            return Result.Failure(new DomainError("RESOURCE_NOT_FOUND", "Loss component not found."));
        }

        _lossComponents.Remove(component);
        RecalculateClaimedAmount();

        return Result.Success();
    }

    private void RecalculateClaimedAmount()
    {
        ClaimedAmount = _lossComponents.Sum(c => c.Amount.Amount);
    }

    public void SetEligibility(
        string status,
        string[] reasonCodes,
        Guid? policyVersionId,
        DateTimeOffset? deadlineAtUtc)
    {
        EligibilityStatus = status;
        EligibilityReasonCodes = reasonCodes;
        PolicyVersionId = policyVersionId;
        ClaimDeadlineAtUtc = deadlineAtUtc;
    }

    public void UpdateReadiness(bool hasMissingMandatoryEvidence)
    {
        if (Status == ClaimStatus.Draft && hasMissingMandatoryEvidence)
        {
            Status = ClaimStatus.EvidencePending;
        }
        else if (Status == ClaimStatus.EvidencePending && !hasMissingMandatoryEvidence)
        {
            Status = ClaimStatus.Draft;
        }
        // In other states, readiness calculation can be informative without regressing the state.
    }

    public Result<ClaimApproval> RequestReview(Guid requestedBy, bool hasMissingMandatoryEvidence, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Draft && Status != ClaimStatus.EvidencePending)
        {
            return Result<ClaimApproval>.Failure(new DomainError("INVALID_STATE_TRANSITION", "Claim must be in draft or evidence pending state to request review."));
        }

        if (ClaimedAmount <= 0)
        {
            return Result<ClaimApproval>.Failure(new DomainError("VALIDATION_FAILED", "Claimed amount must be greater than zero."));
        }

        if (hasMissingMandatoryEvidence)
        {
            return Result<ClaimApproval>.Failure(new DomainError("MANDATORY_EVIDENCE_MISSING", "Mandatory evidence is missing or unscanned."));
        }

        if (ClaimDeadlineAtUtc.HasValue && ClaimDeadlineAtUtc.Value < timeProvider.GetUtcNow())
        {
            return Result<ClaimApproval>.Failure(new DomainError("CLAIM_DEADLINE_EXPIRED", "The claim deadline has expired."));
        }

        if (EligibilityStatus != ClaimEligibilityStatus.Eligible && EligibilityStatus != ClaimEligibilityStatus.ConditionallyEligible)
        {
            return Result<ClaimApproval>.Failure(new DomainError("CLAIM_NOT_ELIGIBLE", "Claim is not eligible for review."));
        }

        var approval = ClaimApproval.CreateSubmissionRequest(TenantId, Id, requestedBy, ConcurrencyStamp, timeProvider);
        _approvals.Add(approval);

        Status = ClaimStatus.ReadyForReview;
        return Result<ClaimApproval>.Success(approval);
    }

    public Result MarkReadyForReview(bool hasMissingMandatoryEvidence, TimeProvider timeProvider)
    {
        var result = RequestReview(Guid.Empty, hasMissingMandatoryEvidence, timeProvider);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public Result ApproveForSubmission(Guid approverId, decimal highValueThreshold, string? note, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.ReadyForReview)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot approve claim in status '{Status}'. Claim must be ReadyForReview."));
        }

        // Enforce Separation of Duties (§26.10):
        // If ClaimedAmount >= highValueThreshold, the approver cannot be the same user who created or requested review.
        if (ClaimedAmount >= highValueThreshold)
        {
            var isCreator = !string.IsNullOrEmpty(CreatedBy) && string.Equals(CreatedBy, approverId.ToString(), StringComparison.OrdinalIgnoreCase);
            var isRequester = _approvals.Any(a => a.ApprovalType == ApprovalType.Submission && a.Status == ApprovalStatus.Pending && a.RequestedBy == approverId);

            if (isCreator || isRequester)
            {
                return Result.Failure(new DomainError("SEPARATION_OF_DUTIES_VIOLATION", "The preparer or review requester cannot approve their own claim when amount meets or exceeds the high-value threshold."));
            }
        }

        var pendingApproval = _approvals.LastOrDefault(a => a.ApprovalType == ApprovalType.Submission && a.Status == ApprovalStatus.Pending);
        if (pendingApproval != null)
        {
            pendingApproval.Approve(approverId, note, timeProvider);
        }

        ApprovedForSubmissionBy = approverId;
        ApprovedForSubmissionAtUtc = timeProvider.GetUtcNow();
        Status = ClaimStatus.ApprovedForSubmission;

        return Result.Success();
    }

    public Result ReturnToDraft(Guid reviewerId, string reason, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.ReadyForReview)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot return claim to draft from status '{Status}'."));
        }

        var pendingApproval = _approvals.LastOrDefault(a => a.ApprovalType == ApprovalType.Submission && a.Status == ApprovalStatus.Pending);
        if (pendingApproval != null)
        {
            pendingApproval.Reject(reviewerId, reason, timeProvider);
        }

        Status = ClaimStatus.Draft;
        return Result.Success();
    }

    public Result RecordSubmission(string externalReference, DateTimeOffset submittedAtUtc, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.ApprovedForSubmission)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot record submission for claim in status '{Status}'. Claim must be ApprovedForSubmission."));
        }

        if (string.IsNullOrWhiteSpace(externalReference))
        {
            return Result.Failure(new DomainError("VALIDATION_FAILED", "External submission reference is required."));
        }

        // Invariant 6 & Edge Case 24: Submission timestamp must not exceed claim deadline
        if (ClaimDeadlineAtUtc.HasValue && submittedAtUtc > ClaimDeadlineAtUtc.Value)
        {
            return Result.Failure(new DomainError("CLAIM_DEADLINE_EXPIRED", "The claim deadline has expired. Claim cannot be submitted after the deadline."));
        }

        ExternalSubmissionReference = externalReference.Trim();
        SubmittedAtUtc = submittedAtUtc;
        Status = ClaimStatus.Submitted;

        return Result.Success();
    }

    public Result<CarrierClaimResponse> RecordAcknowledgement(
        string? carrierReference,
        DateTimeOffset responseAtUtc,
        string? notes,
        Guid recordedBy,
        string sourceChannel,
        TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Submitted && Status != ClaimStatus.UnderReview)
        {
            return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot record carrier acknowledgement for claim in status '{Status}'."));
        }

        var response = CarrierClaimResponse.Create(
            TenantId,
            Id,
            CarrierResponseType.Acknowledged,
            carrierReference,
            responseAtUtc,
            null,
            Currency,
            [],
            notes,
            recordedBy,
            sourceChannel,
            timeProvider);

        _responses.Add(response);
        Status = ClaimStatus.Acknowledged;

        return Result<CarrierClaimResponse>.Success(response);
    }

    public Result<CarrierClaimResponse> RecordInformationRequest(
        string? carrierReference,
        DateTimeOffset responseAtUtc,
        string? notes,
        string[] reasonCodes,
        DateTimeOffset infoDeadlineAtUtc,
        Guid recordedBy,
        string sourceChannel,
        TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Submitted && Status != ClaimStatus.Acknowledged && Status != ClaimStatus.UnderReview)
        {
            return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot record carrier information request for claim in status '{Status}'."));
        }

        var response = CarrierClaimResponse.Create(
            TenantId,
            Id,
            CarrierResponseType.MoreInformationRequested,
            carrierReference,
            responseAtUtc,
            null,
            Currency,
            reasonCodes,
            notes,
            recordedBy,
            sourceChannel,
            timeProvider);

        _responses.Add(response);
        Status = ClaimStatus.MoreInformationRequested;

        return Result<CarrierClaimResponse>.Success(response);
    }

    public Result SupplyAdditionalInformation(string responseNotes, Guid recordedBy, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.MoreInformationRequested)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot supply additional information when claim is in status '{Status}'."));
        }

        Status = ClaimStatus.UnderReview;
        return Result.Success();
    }

    public Result<CarrierClaimResponse> RecordDecision(
        string decisionType,
        decimal? approvedAmount,
        string[] reasonCodes,
        string? carrierReference,
        string? notes,
        DateTimeOffset responseAtUtc,
        Guid recordedBy,
        string sourceChannel,
        TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Submitted &&
            Status != ClaimStatus.Acknowledged &&
            Status != ClaimStatus.UnderReview &&
            Status != ClaimStatus.MoreInformationRequested)
        {
            return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot record carrier decision for claim in status '{Status}'."));
        }

        if (!CarrierResponseType.All.Contains(decisionType))
        {
            return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_DECISION_TYPE", $"Invalid decision type: '{decisionType}'."));
        }

        decimal finalApprovedAmount;
        if (decisionType == CarrierResponseType.Approved)
        {
            finalApprovedAmount = ClaimedAmount;
            ApprovedAmount = finalApprovedAmount;
            Status = ClaimStatus.Approved;
        }
        else if (decisionType == CarrierResponseType.PartiallyApproved)
        {
            if (!approvedAmount.HasValue || approvedAmount.Value <= 0 || approvedAmount.Value >= ClaimedAmount)
            {
                return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_APPROVED_AMOUNT", "Partially approved amount must be greater than zero and less than claimed amount."));
            }

            finalApprovedAmount = approvedAmount.Value;
            ApprovedAmount = finalApprovedAmount;
            Status = ClaimStatus.PartiallyApproved;
        }
        else if (decisionType == CarrierResponseType.Denied)
        {
            finalApprovedAmount = 0m;
            ApprovedAmount = finalApprovedAmount;
            Status = ClaimStatus.Denied;
        }
        else if (decisionType == CarrierResponseType.SettlementOffered)
        {
            if (!approvedAmount.HasValue || approvedAmount.Value <= 0)
            {
                return Result<CarrierClaimResponse>.Failure(new DomainError("INVALID_APPROVED_AMOUNT", "Settlement offered amount must be greater than zero."));
            }

            finalApprovedAmount = approvedAmount.Value;
            ApprovedAmount = finalApprovedAmount;
            Status = ClaimStatus.UnderReview;
        }
        else
        {
            return Result<CarrierClaimResponse>.Failure(new DomainError("UNSUPPORTED_DECISION", $"Decision '{decisionType}' cannot be processed as a final/settlement decision."));
        }

        var response = CarrierClaimResponse.Create(
            TenantId,
            Id,
            decisionType,
            carrierReference,
            responseAtUtc,
            finalApprovedAmount,
            Currency,
            reasonCodes,
            notes,
            recordedBy,
            sourceChannel,
            timeProvider);

        _responses.Add(response);
        return Result<CarrierClaimResponse>.Success(response);
    }

    public Result Appeal(string appealReason, string? notes, Guid appealedBy, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Denied && Status != ClaimStatus.PartiallyApproved)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot appeal claim in status '{Status}'. Only Denied or PartiallyApproved claims can be appealed."));
        }

        Status = ClaimStatus.Appealed;
        return Result.Success();
    }

    public Result MoveToSettlementPending()
    {
        if (Status != ClaimStatus.Approved && Status != ClaimStatus.PartiallyApproved)
        {
            return Result.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                $"Cannot move claim to SettlementPending from status '{Status}'. Only Approved or PartiallyApproved claims can enter settlement pending."));
        }

        Status = ClaimStatus.SettlementPending;
        return Result.Success();
    }

    public Result<RecoveryTransaction> RecordRecovery(
        string transactionType,
        string externalReference,
        decimal amount,
        string currency,
        DateTimeOffset receivedAtUtc,
        Guid recordedBy,
        string? notes,
        TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.SettlementPending &&
            Status != ClaimStatus.Approved &&
            Status != ClaimStatus.PartiallyApproved &&
            Status != ClaimStatus.Paid)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                $"Cannot record recovery for claim in status '{Status}'. Claim must be in Approved, PartiallyApproved, SettlementPending, or Paid status."));
        }

        if (string.IsNullOrWhiteSpace(currency) || !string.Equals(currency.Trim(), Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Result<RecoveryTransaction>.Failure(new DomainError(
                "CURRENCY_MISMATCH",
                $"Transaction currency '{currency}' does not match claim currency '{Currency}'."));
        }

        // Invariant 5: Recovered amount cannot exceed approved amount without an explicit adjustment transaction
        var projectedRecovered = RecoveredAmount + amount;
        if (transactionType != RecoveryTransactionType.Adjustment && projectedRecovered > ApprovedAmount)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError(
                "RECOVERY_EXCEEDS_APPROVED",
                $"Recovered amount ({projectedRecovered}) cannot exceed approved amount ({ApprovedAmount}) without an explicit Adjustment transaction."));
        }

        if (projectedRecovered < 0)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError(
                "NEGATIVE_RECOVERED_AMOUNT",
                "Total recovered amount cannot be negative."));
        }

        var transactionResult = RecoveryTransaction.Create(
            TenantId,
            Id,
            transactionType,
            externalReference,
            amount,
            currency,
            receivedAtUtc,
            recordedBy,
            notes,
            timeProvider);

        if (transactionResult.IsFailure)
        {
            return transactionResult;
        }

        var transaction = transactionResult.Value;
        _recoveryTransactions.Add(transaction);

        // Recalculate recovered amount
        RecoveredAmount = _recoveryTransactions.Sum(t => t.Amount);

        // Lifecycle transitions:
        // If recovered amount reaches or exceeds approved amount, move to Paid; otherwise SettlementPending
        if (RecoveredAmount >= ApprovedAmount && ApprovedAmount > 0)
        {
            Status = ClaimStatus.Paid;
        }
        else
        {
            Status = ClaimStatus.SettlementPending;
        }

        return Result<RecoveryTransaction>.Success(transaction);
    }

    public Result WriteOff(
        decimal writeOffAmount,
        string reason,
        Guid approvedBy,
        TimeProvider timeProvider)
    {
        // Invariant 12: AI cannot execute financial state transitions (enforced at auth & domain boundary)
        if (Status != ClaimStatus.Denied &&
            Status != ClaimStatus.PartiallyApproved &&
            Status != ClaimStatus.SettlementPending &&
            Status != ClaimStatus.Approved)
        {
            return Result.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                $"Cannot write off claim in status '{Status}'. Only Denied, PartiallyApproved, Approved, or SettlementPending claims can be written off."));
        }

        if (writeOffAmount <= 0)
        {
            return Result.Failure(new DomainError(
                "INVALID_AMOUNT",
                "Write-off amount must be greater than zero."));
        }

        if (!WriteOffReasonCodes.IsValid(reason))
        {
            return Result.Failure(new DomainError(
                "INVALID_WRITE_OFF_REASON",
                $"Invalid write-off reason code: '{reason}'."));
        }

        if (approvedBy == Guid.Empty)
        {
            return Result.Failure(new DomainError(
                "INVALID_APPROVER",
                "Approver user ID must not be empty."));
        }

        // Maximum allowable write-off: cannot exceed remaining unrecovered exposure (ClaimedAmount - RecoveredAmount)
        var maxAllowableWriteOff = ClaimedAmount - RecoveredAmount;
        if (WrittenOffAmount + writeOffAmount > maxAllowableWriteOff)
        {
            return Result.Failure(new DomainError(
                "WRITE_OFF_EXCEEDS_EXPOSURE",
                $"Total written-off amount ({WrittenOffAmount + writeOffAmount}) cannot exceed remaining unrecovered exposure ({maxAllowableWriteOff})."));
        }

        WrittenOffAmount += writeOffAmount;
        WrittenOffAtUtc = timeProvider.GetUtcNow();
        WrittenOffBy = approvedBy;
        WriteOffReason = reason;

        return Result.Success();
    }

    public Result Close(
        string? closingNotes,
        Guid closedBy,
        TimeProvider timeProvider)
    {
        if (Status == ClaimStatus.Closed)
        {
            return Result.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                "Claim is already closed."));
        }

        if (Status == ClaimStatus.Cancelled)
        {
            return Result.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                "Cannot close a cancelled claim."));
        }

        if (Status != ClaimStatus.Paid &&
            Status != ClaimStatus.SettlementPending &&
            Status != ClaimStatus.Denied)
        {
            return Result.Failure(new DomainError(
                "INVALID_STATE_TRANSITION",
                $"Cannot close claim in status '{Status}'. Only Paid, SettlementPending, or Denied claims can be closed."));
        }

        if (closedBy == Guid.Empty)
        {
            return Result.Failure(new DomainError(
                "INVALID_CLOSER",
                "ClosedBy user ID must not be empty."));
        }

        // Settlement Definition of Done & Invariant 9:
        // Claim cannot close with unexplained balance (§24 DoD & Invariant 9).
        // Total claimed amount must equal recovered amount + written off amount.
        var unexplainedBalance = ClaimedAmount - (RecoveredAmount + WrittenOffAmount);
        if (unexplainedBalance > 0)
        {
            return Result.Failure(new DomainError(
                "UNEXPLAINED_BALANCE",
                $"Claim cannot close with an unexplained balance of {unexplainedBalance} {Currency}. Any difference between claimed and recovered amounts must be written off prior to closure."));
        }

        Status = ClaimStatus.Closed;
        ClosedAtUtc = timeProvider.GetUtcNow();
        ClosedBy = closedBy;
        ClosingNotes = string.IsNullOrWhiteSpace(closingNotes) ? null : closingNotes.Trim();

        return Result.Success();
    }

    public Result Cancel(string reason, Guid cancelledBy, TimeProvider timeProvider)
    {
        if (Status == ClaimStatus.Submitted ||
            Status == ClaimStatus.Approved ||
            Status == ClaimStatus.SettlementPending ||
            Status == ClaimStatus.Paid ||
            Status == ClaimStatus.Closed)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", $"Cannot cancel claim in status '{Status}'."));
        }

        Status = ClaimStatus.Cancelled;
        ClosedAtUtc = timeProvider.GetUtcNow();
        return Result.Success();
    }
}
