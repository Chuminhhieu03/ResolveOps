using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public class RecordRecoveryHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;

    public RecordRecoveryHandler(
        AppDbContext dbContext,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RecordRecoveryResponse>> HandleAsync(
        RecordRecoveryCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.RecoveryTransactions)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RecordRecoveryResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        // Enforce unique external transaction reference (spec §15.10, Invariant 11, Edge Case 17)
        var referenceExists = await _dbContext.RecoveryTransactions
            .AnyAsync(t => t.ExternalReference == command.ExternalReference.Trim(), cancellationToken);

        if (referenceExists)
        {
            return Result<RecordRecoveryResponse>.Failure(new DomainError(
                "DUPLICATE_EXTERNAL_REFERENCE",
                $"Recovery transaction with external reference '{command.ExternalReference}' already exists. Duplicate payment or credit note imports are rejected."));
        }

        var recordResult = claim.RecordRecovery(
            command.TransactionType,
            command.ExternalReference,
            command.Amount,
            command.Currency,
            command.ReceivedAtUtc,
            command.RecordedBy,
            command.Notes,
            _timeProvider);

        if (recordResult.IsFailure)
        {
            return Result<RecordRecoveryResponse>.Failure(recordResult.Error);
        }

        var recoveryTransaction = recordResult.Value;

        // Atomically publish ClaimRecoveryRecordedV1 outbox event (spec §17.3)
        var integrationEvent = new ClaimRecoveryRecordedV1
        {
            ClaimId = claim.Id,
            RecoveryTransactionId = recoveryTransaction.Id,
            TransactionType = recoveryTransaction.TransactionType,
            ExternalReference = recoveryTransaction.ExternalReference,
            Amount = recoveryTransaction.Amount,
            Currency = recoveryTransaction.Currency,
            ReceivedAtUtc = recoveryTransaction.ReceivedAtUtc
        };

        _outboxWriter.Write(integrationEvent, claim.TenantId, Guid.NewGuid().ToString());

        // Instrument metrics
        ClaimMetrics.ClaimsRecoveryRecordedTotal.Add(1);
        ClaimMetrics.ClaimsRecoveryAmountTotal.Add((double)command.Amount);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var remainingBalance = Math.Max(0m, claim.ApprovedAmount - claim.RecoveredAmount);

        return Result<RecordRecoveryResponse>.Success(new RecordRecoveryResponse(
            claim.Id,
            recoveryTransaction.Id,
            claim.Status,
            recoveryTransaction.TransactionType,
            recoveryTransaction.ExternalReference,
            recoveryTransaction.Amount,
            recoveryTransaction.Currency,
            claim.RecoveredAmount,
            claim.ApprovedAmount,
            remainingBalance,
            recoveryTransaction.ReceivedAtUtc));
    }
}
