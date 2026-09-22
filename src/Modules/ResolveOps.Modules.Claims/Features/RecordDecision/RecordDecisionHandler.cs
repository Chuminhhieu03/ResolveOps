using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public class RecordDecisionHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;

    public RecordDecisionHandler(
        AppDbContext dbContext,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RecordDecisionResponse>> HandleAsync(
        RecordDecisionCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RecordDecisionResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var result = claim.RecordDecision(
            command.Decision,
            command.ApprovedAmount,
            command.ReasonCodes ?? [],
            command.CarrierReference,
            command.Notes,
            command.ResponseAtUtc,
            command.RecordedBy,
            command.SourceChannel,
            _timeProvider);

        if (result.IsFailure)
        {
            return Result<RecordDecisionResponse>.Failure(result.Error);
        }

        var response = result.Value;

        // Atomically publish ClaimDecisionRecordedV1 via outbox (spec §17.3)
        var integrationEvent = new ClaimDecisionRecordedV1
        {
            ClaimId = claim.Id,
            Decision = command.Decision,
            ApprovedAmount = claim.ApprovedAmount,
            Currency = claim.Currency,
            RecordedAtUtc = _timeProvider.GetUtcNow()
        };

        _outboxWriter.Write(integrationEvent, claim.TenantId, Guid.NewGuid().ToString());

        ClaimMetrics.ClaimsDecisionsRecordedTotal.Add(1);
        if (claim.ApprovedAmount > 0)
        {
            ClaimMetrics.ClaimsAmountApprovedTotal.Add((double)claim.ApprovedAmount);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var deniedDifference = Math.Max(0m, claim.ClaimedAmount - claim.ApprovedAmount);

        return Result<RecordDecisionResponse>.Success(new RecordDecisionResponse(
            claim.Id,
            claim.Status,
            claim.ApprovedAmount,
            deniedDifference,
            claim.ClaimedAmount,
            response.Id,
            response.ResponseAtUtc));
    }
}
