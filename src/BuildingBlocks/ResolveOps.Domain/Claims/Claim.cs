using System;
using System.Collections.Generic;
using System.Linq;
using ResolveOps.Domain.Claims.Enums;
using ResolveOps.Domain.Claims.ValueObjects;

namespace ResolveOps.Domain.Claims;

public class Claim : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ClaimNumber { get; private set; } = string.Empty;
    public Guid CaseId { get; private set; }
    public Guid CarrierId { get; private set; }
    public ClaimType ClaimType { get; private set; }
    public ClaimStatus Status { get; private set; }

    public ClaimEligibilityStatus EligibilityStatus { get; private set; }
    public string[] EligibilityReasonCodes { get; private set; } = [];
    public Guid? PolicyVersionId { get; private set; }
    public DateTimeOffset? ClaimDeadlineAtUtc { get; private set; }

    public decimal ClaimedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal RecoveredAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public string? ExternalSubmissionReference { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedForSubmissionBy { get; private set; }
    public DateTimeOffset? ApprovedForSubmissionAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;

    private readonly List<ClaimLossComponent> _lossComponents = [];
    public IReadOnlyCollection<ClaimLossComponent> LossComponents => _lossComponents.AsReadOnly();

    private Claim() { } // EF Core

    public static Result<Claim> CreateDraft(
        Guid id,
        Guid tenantId,
        string claimNumber,
        Guid caseId,
        Guid carrierId,
        ClaimType claimType,
        string currency,
        ClaimEligibilityStatus eligibilityStatus,
        string[] eligibilityReasonCodes,
        Guid? policyVersionId,
        DateTimeOffset? deadlineAtUtc)
    {
        if (eligibilityStatus == ClaimEligibilityStatus.NotEligible)
        {
            return Result<Claim>.Failure(new DomainError("CLAIM_NOT_ELIGIBLE", "Case is not eligible for this claim type."));
        }

        var claim = new Claim
        {
            Id = id,
            TenantId = tenantId,
            ClaimNumber = claimNumber,
            CaseId = caseId,
            CarrierId = carrierId,
            ClaimType = claimType,
            Currency = currency.ToUpperInvariant(),
            Status = ClaimStatus.Draft,
            EligibilityStatus = eligibilityStatus,
            EligibilityReasonCodes = eligibilityReasonCodes,
            PolicyVersionId = policyVersionId,
            ClaimDeadlineAtUtc = deadlineAtUtc,
            ClaimedAmount = 0,
            ApprovedAmount = 0,
            RecoveredAmount = 0
        };

        return Result<Claim>.Success(claim);
    }

    public Result<ClaimLossComponent> AddLossComponent(
        LossComponentType componentType,
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
            TenantId,
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
        ClaimEligibilityStatus status,
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

    public Result MarkReadyForReview(bool hasMissingMandatoryEvidence, TimeProvider timeProvider)
    {
        if (Status != ClaimStatus.Draft && ClaimStatus.EvidencePending != Status)
        {
            return Result.Failure(new DomainError("INVALID_STATE_TRANSITION", "Claim must be in draft to request review."));
        }

        if (ClaimedAmount <= 0)
        {
            return Result.Failure(new DomainError("VALIDATION_FAILED", "Claimed amount must be greater than zero."));
        }

        if (hasMissingMandatoryEvidence)
        {
            return Result.Failure(new DomainError("MANDATORY_EVIDENCE_MISSING", "Mandatory evidence is missing or unscanned."));
        }

        if (ClaimDeadlineAtUtc.HasValue && ClaimDeadlineAtUtc.Value < timeProvider.GetUtcNow())
        {
            return Result.Failure(new DomainError("CLAIM_DEADLINE_EXPIRED", "The claim deadline has expired."));
        }

        if (EligibilityStatus != ClaimEligibilityStatus.Eligible && EligibilityStatus != ClaimEligibilityStatus.ConditionallyEligible)
        {
            return Result.Failure(new DomainError("CLAIM_NOT_ELIGIBLE", "Claim is not eligible for review."));
        }

        Status = ClaimStatus.ReadyForReview;
        return Result.Success();
    }
}
