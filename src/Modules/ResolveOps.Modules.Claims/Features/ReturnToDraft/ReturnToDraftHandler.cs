using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.ReturnToDraft;

public class ReturnToDraftHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ReturnToDraftHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReturnToDraftResponse>> HandleAsync(
        ReturnToDraftCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<ReturnToDraftResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.ReturnToDraft(command.ReviewerId, command.Reason, _timeProvider);
        if (result.IsFailure)
        {
            return Result<ReturnToDraftResponse>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ReturnToDraftResponse>.Success(new ReturnToDraftResponse(
            claim.Id,
            claim.Status,
            command.Reason));
    }
}
