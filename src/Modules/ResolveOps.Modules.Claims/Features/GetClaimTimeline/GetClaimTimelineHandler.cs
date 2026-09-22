using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.GetClaimTimeline;

public class GetClaimTimelineHandler
{
    private readonly AppDbContext _dbContext;

    public GetClaimTimelineHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetClaimTimelineResponse>> HandleAsync(
        GetClaimTimelineQuery query,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Approvals)
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<GetClaimTimelineResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var items = new List<ClaimTimelineItem>();

        // 1. Claim creation
        items.Add(new ClaimTimelineItem(
            "ClaimDraftCreated",
            claim.CreatedAtUtc,
            $"Claim {claim.ClaimNumber} drafted with initial eligibility: {claim.EligibilityStatus}",
            claim.CreatedBy,
            JsonSerializer.Serialize(new { claim.ClaimType, claim.Currency, claim.ClaimedAmount })));

        // 2. Approvals
        foreach (var approval in claim.Approvals)
        {
            items.Add(new ClaimTimelineItem(
                "ReviewRequested",
                approval.RequestedAtUtc,
                $"Review requested for approval type: {approval.ApprovalType}",
                approval.RequestedBy.ToString(),
                null));

            if (approval.DecidedAtUtc.HasValue)
            {
                var eventType = approval.Status == ApprovalStatus.Approved
                    ? "SubmissionApproved"
                    : "ReturnedToDraft";

                items.Add(new ClaimTimelineItem(
                    eventType,
                    approval.DecidedAtUtc.Value,
                    $"Approval {approval.ApprovalType} was {approval.Status}. Note: {approval.DecisionNote ?? "None"}",
                    approval.DecidedBy?.ToString(),
                    JsonSerializer.Serialize(new { approval.Status, approval.DecisionNote })));
            }
        }

        // 3. Submission
        if (claim.SubmittedAtUtc.HasValue)
        {
            items.Add(new ClaimTimelineItem(
                "ClaimSubmitted",
                claim.SubmittedAtUtc.Value,
                $"Claim formally submitted to carrier with reference: {claim.ExternalSubmissionReference}",
                null,
                JsonSerializer.Serialize(new { claim.ExternalSubmissionReference, claim.ClaimedAmount, claim.Currency })));
        }

        // 4. Carrier Responses
        foreach (var resp in claim.Responses)
        {
            items.Add(new ClaimTimelineItem(
                $"CarrierResponse_{resp.ResponseType}",
                resp.ResponseAtUtc,
                $"Carrier responded with {resp.ResponseType}. Ref: {resp.CarrierReference ?? "N/A"}. Note: {resp.Notes ?? "None"}",
                resp.RecordedBy.ToString(),
                JsonSerializer.Serialize(new
                {
                    resp.ResponseType,
                    resp.CarrierReference,
                    resp.ApprovedAmount,
                    resp.ReasonCodes,
                    resp.SourceChannel
                })));
        }

        // 5. Closed / Cancelled
        if (claim.ClosedAtUtc.HasValue)
        {
            items.Add(new ClaimTimelineItem(
                $"Claim_{claim.Status}",
                claim.ClosedAtUtc.Value,
                $"Claim entered terminal status: {claim.Status}",
                null,
                null));
        }

        var sortedTimeline = items.OrderBy(i => i.TimestampUtc).ToList();

        return Result<GetClaimTimelineResponse>.Success(new GetClaimTimelineResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.Status,
            sortedTimeline));
    }
}
