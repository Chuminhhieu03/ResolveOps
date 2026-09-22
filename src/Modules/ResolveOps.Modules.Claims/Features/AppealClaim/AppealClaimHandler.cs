using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.AppealClaim;

public class AppealClaimHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AppealClaimHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AppealClaimResponse>> HandleAsync(
        AppealClaimCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<AppealClaimResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.Appeal(command.AppealReason, command.Notes, command.AppealedBy, _timeProvider);
        if (result.IsFailure)
        {
            return Result<AppealClaimResponse>.Failure(result.Error);
        }

        ClaimMetrics.ClaimsAppealedTotal.Add(1);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppealClaimResponse>.Success(new AppealClaimResponse(
            claim.Id,
            claim.Status,
            command.AppealReason,
            _timeProvider.GetUtcNow()));
    }
}
