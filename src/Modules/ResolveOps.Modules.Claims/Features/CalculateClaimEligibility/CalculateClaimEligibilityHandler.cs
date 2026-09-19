using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Claims.Eligibility;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.CalculateClaimEligibility;

public class CalculateClaimEligibilityHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IClaimEligibilityEvaluator _evaluator;

    public CalculateClaimEligibilityHandler(
        DbContext dbContext,
        ITenantContext tenantContext,
        IClaimEligibilityEvaluator evaluator)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _evaluator = evaluator;
    }

    public async Task<Result<CalculateClaimEligibilityResponse>> HandleAsync(
        CalculateClaimEligibilityCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId && c.TenantId == tenantId, cancellationToken);

        if (claim == null)
        {
            return Result<CalculateClaimEligibilityResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var exceptionCase = await _dbContext.Set<ExceptionCase>()
            .FirstOrDefaultAsync(c => c.Id == claim.CaseId && c.TenantId == tenantId, cancellationToken);

        if (exceptionCase == null)
        {
            return Result<CalculateClaimEligibilityResponse>.Failure(new DomainError("CASE_NOT_FOUND", "Associated exception case not found."));
        }

        var eligibilityResult = await _evaluator.EvaluateAsync(
            exceptionCase,
            claim.CarrierId,
            claim.ClaimType,
            cancellationToken);

        claim.SetEligibility(
            eligibilityResult.Status,
            eligibilityResult.ReasonCodes,
            eligibilityResult.PolicyVersionId,
            eligibilityResult.DeadlineAtUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CalculateClaimEligibilityResponse(
            eligibilityResult.Status.ToString(),
            eligibilityResult.ReasonCodes,
            eligibilityResult.DeadlineAtUtc);
    }
}
