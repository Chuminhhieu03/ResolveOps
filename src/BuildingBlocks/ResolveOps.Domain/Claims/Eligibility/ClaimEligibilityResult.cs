using System;
using ResolveOps.Domain.Claims.Enums;

namespace ResolveOps.Domain.Claims.Eligibility;

public record ClaimEligibilityResult(
    ClaimEligibilityStatus Status,
    string[] ReasonCodes,
    DateTimeOffset? DeadlineAtUtc,
    Guid? PolicyVersionId);
