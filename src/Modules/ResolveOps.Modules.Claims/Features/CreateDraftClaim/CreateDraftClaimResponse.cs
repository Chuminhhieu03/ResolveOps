using System;
using ResolveOps.Domain.Claims.Enums;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public record CreateDraftClaimResponse(
    Guid Id,
    string ClaimNumber,
    ClaimEligibilityStatus EligibilityStatus,
    string[] EligibilityReasonCodes,
    DateTimeOffset? DeadlineAtUtc);
