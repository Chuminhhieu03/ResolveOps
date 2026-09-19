using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceChecklist;

internal sealed class GetEvidenceChecklistHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetEvidenceChecklistHandler(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<EvidenceChecklistResponse>> HandleAsync(
        Guid claimId,
        CancellationToken cancellationToken)
    {
        // Phase 10 will introduce Claims; for now, we look up a linked EvidenceDocument
        // to resolve the case and exception type. If claimId doesn't map yet, return 404.
        // The endpoint URL is part of Phase 9 spec; the checklist calculation is fully deterministic.

        var tenantId = _tenantContext.TenantId.Value;

        // Find any document linked to this claim (via ClaimId) to discover CaseId and ExceptionType.
        var linkedDocument = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ClaimId == claimId, cancellationToken);

        Guid caseId;
        string? exceptionType = null;

        if (linkedDocument is not null)
        {
            caseId = linkedDocument.CaseId;

            // Resolve exception type from the case.
            var exCase = await _dbContext.ExceptionCases
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

            exceptionType = exCase?.ExceptionType;
        }
        else
        {
            // No documents linked yet; try to resolve caseId from ExceptionCase (Phase 10 will refine).
            return DomainError.Failure(
                "ERR_CLAIM_NOT_FOUND",
                $"Claim '{claimId}' was not found or has no associated evidence.");
        }

        // Load requirements for this exception type.
        var requirements = await _dbContext.EvidenceRequirements
            .AsNoTracking()
            .Where(r => r.ExceptionType == exceptionType
                     && (r.ClaimType == null))
            .ToListAsync(cancellationToken);

        // Load Available documents for this case, newest version first.
        var availableDocs = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .Where(d => d.CaseId == caseId && d.Status == DocumentStatus.Available)
            .OrderByDescending(d => d.VersionNumber)
            .ThenByDescending(d => d.UploadedAtUtc)
            .Select(d => new { d.Id, d.EvidenceType })
            .ToListAsync(cancellationToken);

        var docsByType = availableDocs
            .GroupBy(d => d.EvidenceType)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var items = requirements.Select(req =>
        {
            var satisfied = docsByType.TryGetValue(req.EvidenceType, out var docId);
            return new ChecklistItem(
                EvidenceType: req.EvidenceType,
                IsMandatory: req.IsMandatory,
                IsSatisfied: satisfied,
                SatisfiedByDocumentId: satisfied ? docId : null,
                UnsatisfiedReason: satisfied ? null : (req.IsMandatory ? "Required document not yet available." : "Optional document not present."));
        }).ToList();

        var isReady = items.All(i => !i.IsMandatory || i.IsSatisfied);

        return new EvidenceChecklistResponse(
            CaseId: caseId,
            ClaimId: claimId,
            IsReady: isReady,
            Items: items);
    }
}
