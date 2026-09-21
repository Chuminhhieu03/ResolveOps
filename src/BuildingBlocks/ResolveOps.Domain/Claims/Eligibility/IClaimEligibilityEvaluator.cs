using System;
using System.Threading;
using System.Threading.Tasks;

using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Domain.Claims.Eligibility;

public interface IClaimEligibilityEvaluator
{
    Task<ClaimEligibilityResult> EvaluateAsync(
        ExceptionCase exceptionCase,
        Guid carrierId,
        string claimType,
        CancellationToken cancellationToken = default);
}
