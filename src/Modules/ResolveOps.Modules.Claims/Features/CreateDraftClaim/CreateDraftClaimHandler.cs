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

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public class CreateDraftClaimHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IClaimEligibilityEvaluator _eligibilityEvaluator;

    public CreateDraftClaimHandler(
        DbContext dbContext,
        ITenantContext tenantContext,
        IClaimEligibilityEvaluator eligibilityEvaluator)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _eligibilityEvaluator = eligibilityEvaluator;
    }

    public async Task<Result<CreateDraftClaimResponse>> HandleAsync(
        CreateDraftClaimCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var exceptionCase = await _dbContext.Set<ExceptionCase>()
            .FirstOrDefaultAsync(c => c.Id == command.CaseId, cancellationToken);

        if (exceptionCase == null)
        {
            return Result<CreateDraftClaimResponse>.Failure(new DomainError("CASE_NOT_FOUND", "The specified exception case was not found."));
        }

        var eligibilityResult = await _eligibilityEvaluator.EvaluateAsync(
            exceptionCase,
            command.CarrierId,
            command.ClaimType,
            cancellationToken);

        var claimNumber = $"CLM-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
        var claimId = Guid.NewGuid();

        var claimResult = Claim.CreateDraft(
            claimId,
            tenantId,
            claimNumber,
            command.CaseId,
            command.CarrierId,
            command.ClaimType,
            command.Currency,
            eligibilityResult.Status,
            eligibilityResult.ReasonCodes,
            eligibilityResult.PolicyVersionId,
            eligibilityResult.DeadlineAtUtc);

        if (claimResult.IsFailure)
        {
            return Result<CreateDraftClaimResponse>.Failure(claimResult.Error);
        }

        var claim = claimResult.Value;
        _dbContext.Set<Claim>().Add(claim);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateDraftClaimResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.EligibilityStatus,
            claim.EligibilityReasonCodes,
            claim.ClaimDeadlineAtUtc);
    }
}
