using System;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain.Claims.Enums;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Domain.Claims.Eligibility;

public interface IClaimEligibilityEvaluator
{
    Task<ClaimEligibilityResult> EvaluateAsync(
        ExceptionCase exceptionCase,
        Guid carrierId,
        ClaimType claimType,
        CancellationToken cancellationToken = default);
}
