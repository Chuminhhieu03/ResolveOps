using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.GetClaimById;

public class GetClaimByIdHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetClaimByIdHandler(DbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetClaimByIdResponse>> HandleAsync(
        GetClaimByIdQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .Include(c => c.LossComponents)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<GetClaimByIdResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var lossComponents = claim.LossComponents.Select(lc => new LossComponentResponse(
            lc.Id,
            lc.ComponentType.ToString(),
            lc.Description,
            lc.Quantity,
            lc.UnitAmount,
            lc.Amount.Amount,
            lc.Amount.Currency,
            lc.SourceDocumentId)).ToList();

        var response = new GetClaimByIdResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.CaseId,
            claim.CarrierId,
            claim.ClaimType.ToString(),
            claim.Status.ToString(),
            claim.EligibilityStatus.ToString(),
            claim.EligibilityReasonCodes,
            claim.ClaimDeadlineAtUtc,
            claim.ClaimedAmount,
            claim.ApprovedAmount,
            claim.RecoveredAmount,
            claim.Currency,
            claim.ExternalSubmissionReference,
            claim.SubmittedAtUtc,
            claim.CreatedAtUtc,
            lossComponents.AsReadOnly());

        return Result<GetClaimByIdResponse>.Success(response);
    }
}
