using System;


namespace ResolveOps.Domain.Claims.Eligibility;

public record ClaimEligibilityResult(
    string Status,
    string[] ReasonCodes,
    DateTimeOffset? DeadlineAtUtc,
    Guid? PolicyVersionId);
