using System;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public record CreateDraftClaimResponse(
    Guid Id,
    string ClaimNumber,
    string EligibilityStatus,
    string[] EligibilityReasonCodes,
    DateTimeOffset? DeadlineAtUtc);
