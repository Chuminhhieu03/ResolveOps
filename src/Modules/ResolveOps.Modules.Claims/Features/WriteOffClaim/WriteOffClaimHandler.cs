using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public class WriteOffClaimHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public WriteOffClaimHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<WriteOffClaimResponse>> HandleAsync(
        WriteOffClaimCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<WriteOffClaimResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.WriteOff(
            command.WriteOffAmount,
            command.Reason,
            command.ApprovedBy,
            _timeProvider);

        if (result.IsFailure)
        {
            return Result<WriteOffClaimResponse>.Failure(result.Error);
        }

        ClaimMetrics.ClaimsWrittenOffTotal.Add(1);
        ClaimMetrics.ClaimsWrittenOffAmountTotal.Add((double)command.WriteOffAmount);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var remainingExposure = Math.Max(0m, claim.ClaimedAmount - (claim.RecoveredAmount + claim.WrittenOffAmount));

        return Result<WriteOffClaimResponse>.Success(new WriteOffClaimResponse(
            claim.Id,
            claim.Status,
            claim.ClaimedAmount,
            claim.ApprovedAmount,
            claim.RecoveredAmount,
            claim.WrittenOffAmount,
            remainingExposure,
            claim.WriteOffReason ?? string.Empty,
            claim.WrittenOffAtUtc ?? _timeProvider.GetUtcNow(),
            claim.WrittenOffBy ?? command.ApprovedBy));
    }
}
