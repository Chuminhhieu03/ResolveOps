using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Claims.Readiness;
using ResolveOps.Domain.Claims;

using ResolveOps.Domain.Documents;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;

namespace ResolveOps.Application.Claims.Readiness;

public class ClaimReadinessEvaluator : IClaimReadinessEvaluator
{
    private readonly DbContext _dbContext;

    public ClaimReadinessEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClaimReadinessResult> EvaluateAsync(
        Claim claim,
        ExceptionCase exceptionCase,
        CancellationToken cancellationToken = default)
    {
        // Get available documents for this case
        var availableDocs = await _dbContext.Set<EvidenceDocument>()
            .Where(d => d.CaseId == claim.CaseId && d.Status == DocumentStatus.Available)
            .ToListAsync(cancellationToken);

        // Get evidence requirements for this claim type and exception type
        var requirements = await _dbContext.Set<EvidenceRequirement>()
            .Where(r => r.ClaimType == claim.ClaimType.ToString() && r.ExceptionType == exceptionCase.ExceptionType.ToString())
            .ToListAsync(cancellationToken);

        var checklist = new List<EvidenceRequirementCheckItem>();
        var missingMandatory = new List<string>();
        bool isReady = true;

        foreach (var req in requirements)
        {
            var matchingDoc = availableDocs.FirstOrDefault(d => d.EvidenceType == req.EvidenceType);
            bool isSatisfied = matchingDoc != null;

            checklist.Add(new EvidenceRequirementCheckItem(
                matchingDoc?.Id,
                req.EvidenceType,
                req.IsMandatory,
                isSatisfied));

            if (req.IsMandatory && !isSatisfied)
            {
                missingMandatory.Add(req.EvidenceType);
                isReady = false;
            }
        }

        return new ClaimReadinessResult(
            isReady,
            checklist.AsReadOnly(),
            missingMandatory.ToArray());
    }
}
