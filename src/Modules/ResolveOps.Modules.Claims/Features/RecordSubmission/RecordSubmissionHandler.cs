using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public class RecordSubmissionHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;

    public RecordSubmissionHandler(
        AppDbContext dbContext,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RecordSubmissionResponse>> HandleAsync(
        RecordSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RecordSubmissionResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        // Duplicate reference check for same carrier (spec §10.5 Invariant 10)
        var referenceExists = await _dbContext.Claims.AnyAsync(
            c => c.CarrierId == claim.CarrierId &&
                 c.ExternalSubmissionReference == command.ExternalReference.Trim() &&
                 c.Id != claim.Id,
            cancellationToken);

        if (referenceExists)
        {
            return Result<RecordSubmissionResponse>.Failure(new DomainError("DUPLICATE_SUBMISSION_REFERENCE", "A claim with this external submission reference already exists for the carrier."));
        }

        var result = claim.RecordSubmission(command.ExternalReference, command.SubmittedAtUtc, _timeProvider);
        if (result.IsFailure)
        {
            return Result<RecordSubmissionResponse>.Failure(result.Error);
        }

        // Outbox event published atomically with claim state transition
        var integrationEvent = new ClaimSubmittedV1
        {
            ClaimId = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            CaseId = claim.CaseId,
            CarrierId = claim.CarrierId,
            ClaimedAmount = claim.ClaimedAmount,
            Currency = claim.Currency,
            SubmittedAtUtc = claim.SubmittedAtUtc!.Value,
            DeadlineAtUtc = claim.ClaimDeadlineAtUtc ?? claim.SubmittedAtUtc!.Value.AddDays(30)
        };

        _outboxWriter.Write(integrationEvent, claim.TenantId, Guid.NewGuid().ToString());

        ClaimMetrics.ClaimsSubmittedTotal.Add(1);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RecordSubmissionResponse>.Success(new RecordSubmissionResponse(
            claim.Id,
            claim.Status,
            claim.ExternalSubmissionReference!,
            claim.SubmittedAtUtc!.Value));
    }
}
