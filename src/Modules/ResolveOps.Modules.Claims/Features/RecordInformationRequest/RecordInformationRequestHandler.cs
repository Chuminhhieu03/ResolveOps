using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public class RecordInformationRequestHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RecordInformationRequestHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RecordInformationRequestResponse>> HandleAsync(
        RecordInformationRequestCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RecordInformationRequestResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.RecordInformationRequest(
            command.CarrierReference,
            command.ResponseAtUtc,
            command.Notes,
            command.ReasonCodes,
            command.InfoDeadlineAtUtc,
            command.RecordedBy,
            command.SourceChannel,
            _timeProvider);

        if (result.IsFailure)
        {
            return Result<RecordInformationRequestResponse>.Failure(result.Error);
        }

        var response = result.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RecordInformationRequestResponse>.Success(new RecordInformationRequestResponse(
            claim.Id,
            claim.Status,
            response.Id,
            command.InfoDeadlineAtUtc));
    }
}
