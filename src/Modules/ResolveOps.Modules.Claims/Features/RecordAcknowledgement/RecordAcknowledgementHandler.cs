using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public class RecordAcknowledgementHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RecordAcknowledgementHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RecordAcknowledgementResponse>> HandleAsync(
        RecordAcknowledgementCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RecordAcknowledgementResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.RecordAcknowledgement(
            command.CarrierReference,
            command.ResponseAtUtc,
            command.Notes,
            command.RecordedBy,
            command.SourceChannel,
            _timeProvider);

        if (result.IsFailure)
        {
            return Result<RecordAcknowledgementResponse>.Failure(result.Error);
        }

        var response = result.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RecordAcknowledgementResponse>.Success(new RecordAcknowledgementResponse(
            claim.Id,
            claim.Status,
            response.Id,
            response.ResponseAtUtc));
    }
}
