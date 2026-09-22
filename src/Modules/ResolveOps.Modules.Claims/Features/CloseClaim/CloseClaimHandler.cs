using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.CloseClaim;

public class CloseClaimHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CloseClaimHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CloseClaimResponse>> HandleAsync(
        CloseClaimCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<CloseClaimResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.Close(
            command.ClosingNotes,
            command.ClosedBy,
            _timeProvider);

        if (result.IsFailure)
        {
            return Result<CloseClaimResponse>.Failure(result.Error);
        }

        ClaimMetrics.ClaimsClosedTotal.Add(1);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CloseClaimResponse>.Success(new CloseClaimResponse(
            claim.Id,
            claim.Status,
            claim.ClaimedAmount,
            claim.ApprovedAmount,
            claim.RecoveredAmount,
            claim.WrittenOffAmount,
            claim.ClosedAtUtc ?? _timeProvider.GetUtcNow(),
            claim.ClosedBy ?? command.ClosedBy,
            claim.ClosingNotes));
    }
}
