using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.SupplyAdditionalInformation;

public class SupplyAdditionalInformationHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SupplyAdditionalInformationHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SupplyAdditionalInformationResponse>> HandleAsync(
        SupplyAdditionalInformationCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<SupplyAdditionalInformationResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.SupplyAdditionalInformation(command.ResponseNotes, command.RecordedBy, _timeProvider);
        if (result.IsFailure)
        {
            return Result<SupplyAdditionalInformationResponse>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SupplyAdditionalInformationResponse>.Success(new SupplyAdditionalInformationResponse(
            claim.Id,
            claim.Status));
    }
}
