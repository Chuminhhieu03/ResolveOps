using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.CancelClaim;

public class CancelClaimHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CancelClaimHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CancelClaimResponse>> HandleAsync(
        CancelClaimCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<CancelClaimResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.Cancel(command.Reason, command.CancelledBy, _timeProvider);
        if (result.IsFailure)
        {
            return Result<CancelClaimResponse>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CancelClaimResponse>.Success(new CancelClaimResponse(
            claim.Id,
            claim.Status,
            command.Reason,
            _timeProvider.GetUtcNow()));
    }
}
