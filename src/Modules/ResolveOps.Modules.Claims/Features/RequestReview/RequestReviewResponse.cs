using System;

namespace ResolveOps.Modules.Claims.Features.RequestReview;

public record RequestReviewResponse(
    Guid ClaimId,
    string Status,
    Guid ApprovalId,
    string ApprovalStatus,
    DateTimeOffset RequestedAtUtc);
