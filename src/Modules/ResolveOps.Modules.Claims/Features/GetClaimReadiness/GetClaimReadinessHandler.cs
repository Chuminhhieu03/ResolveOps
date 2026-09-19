using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Claims.Readiness;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.GetClaimReadiness;

public class GetClaimReadinessHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IClaimReadinessEvaluator _evaluator;

    public GetClaimReadinessHandler(
        DbContext dbContext,
        ITenantContext tenantContext,
        IClaimReadinessEvaluator evaluator)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _evaluator = evaluator;
    }

    public async Task<Result<GetClaimReadinessResponse>> HandleAsync(
        GetClaimReadinessQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId && c.TenantId == tenantId, cancellationToken);

        if (claim == null)
        {
            return Result<GetClaimReadinessResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var exceptionCase = await _dbContext.Set<ExceptionCase>()
            .FirstOrDefaultAsync(c => c.Id == claim.CaseId && c.TenantId == tenantId, cancellationToken);

        if (exceptionCase == null)
        {
            return Result<GetClaimReadinessResponse>.Failure(new DomainError("CASE_NOT_FOUND", "Associated exception case not found."));
        }

        var readinessResult = await _evaluator.EvaluateAsync(claim, exceptionCase, cancellationToken);

        var checklist = readinessResult.Checklist.Select(item => new EvidenceCheckItemResponse(
            item.DocumentId,
            item.EvidenceType,
            item.IsMandatory,
            item.IsSatisfied)).ToList();

        return new GetClaimReadinessResponse(
            readinessResult.IsReady,
            checklist.AsReadOnly(),
            readinessResult.MissingRequirements);
    }
}
