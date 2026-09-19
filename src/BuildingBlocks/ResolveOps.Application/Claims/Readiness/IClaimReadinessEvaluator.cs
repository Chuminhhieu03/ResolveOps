using System;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Application.Claims.Readiness;

public interface IClaimReadinessEvaluator
{
    Task<ClaimReadinessResult> EvaluateAsync(
        Claim claim,
        ExceptionCase exceptionCase,
        CancellationToken cancellationToken = default);
}
