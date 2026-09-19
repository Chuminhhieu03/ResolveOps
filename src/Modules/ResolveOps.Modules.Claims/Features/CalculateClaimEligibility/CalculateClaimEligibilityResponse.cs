using System;
using ResolveOps.Domain.Claims.Enums;

namespace ResolveOps.Modules.Claims.Features.CalculateClaimEligibility;

public record CalculateClaimEligibilityResponse(
    string EligibilityStatus,
    string[] ReasonCodes,
    DateTimeOffset? DeadlineAtUtc);
