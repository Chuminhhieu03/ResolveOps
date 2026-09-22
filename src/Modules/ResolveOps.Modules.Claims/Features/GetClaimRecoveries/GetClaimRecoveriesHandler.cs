using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.GetClaimRecoveries;

public class GetClaimRecoveriesHandler
{
    private readonly AppDbContext _dbContext;

    public GetClaimRecoveriesHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetClaimRecoveriesResponse>> HandleAsync(
        GetClaimRecoveriesQuery query,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.RecoveryTransactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<GetClaimRecoveriesResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var outstandingBalance = Math.Max(0m, claim.ClaimedAmount - (claim.RecoveredAmount + claim.WrittenOffAmount));

        var transactions = claim.RecoveryTransactions
            .OrderByDescending(t => t.ReceivedAtUtc)
            .Select(t => new RecoveryTransactionItem(
                t.Id,
                t.TransactionType,
                t.ExternalReference,
                t.Amount,
                t.Currency,
                t.ReceivedAtUtc,
                t.RecordedBy,
                t.Notes,
                t.CreatedAtUtc))
            .ToList();

        return Result<GetClaimRecoveriesResponse>.Success(new GetClaimRecoveriesResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.Status,
            claim.Currency,
            claim.ClaimedAmount,
            claim.ApprovedAmount,
            claim.RecoveredAmount,
            claim.WrittenOffAmount,
            outstandingBalance,
            claim.WriteOffReason,
            claim.WrittenOffAtUtc,
            transactions));
    }
}
