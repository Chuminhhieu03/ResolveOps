using System;

namespace ResolveOps.Domain.Claims;

public sealed class RecoveryTransaction : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string TransactionType { get; private set; } = string.Empty;
    public string ExternalReference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public Guid RecordedBy { get; private set; }
    public string? Notes { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private RecoveryTransaction() { } // EF Core

    public static Result<RecoveryTransaction> Create(
        Guid tenantId,
        Guid claimId,
        string transactionType,
        string externalReference,
        decimal amount,
        string currency,
        DateTimeOffset receivedAtUtc,
        Guid recordedBy,
        string? notes,
        TimeProvider timeProvider)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_TENANT", "Tenant ID must not be empty."));
        }

        if (claimId == Guid.Empty)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_CLAIM", "Claim ID must not be empty."));
        }

        if (recordedBy == Guid.Empty)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_RECORDER", "RecordedBy user ID must not be empty."));
        }

        if (!RecoveryTransactionType.IsValid(transactionType))
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_TRANSACTION_TYPE", $"Invalid recovery transaction type: '{transactionType}'."));
        }

        if (string.IsNullOrWhiteSpace(externalReference))
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_EXTERNAL_REFERENCE", "External transaction reference is required."));
        }

        if (externalReference.Trim().Length > 150)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_EXTERNAL_REFERENCE", "External transaction reference must not exceed 150 characters."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_CURRENCY", "Currency must be a 3-character ISO code."));
        }

        if (transactionType == RecoveryTransactionType.Payment || transactionType == RecoveryTransactionType.CreditNote)
        {
            if (amount <= 0)
            {
                return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_AMOUNT", $"{transactionType} amount must be greater than zero."));
            }
        }
        else if (transactionType == RecoveryTransactionType.Adjustment)
        {
            if (amount == 0)
            {
                return Result<RecoveryTransaction>.Failure(new DomainError("INVALID_AMOUNT", "Adjustment amount cannot be zero."));
            }
        }

        var now = timeProvider.GetUtcNow();

        var transaction = new RecoveryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClaimId = claimId,
            TransactionType = transactionType,
            ExternalReference = externalReference.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            ReceivedAtUtc = receivedAtUtc,
            RecordedBy = recordedBy,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAtUtc = now,
            CreatedBy = recordedBy.ToString(),
            UpdatedAtUtc = now,
            UpdatedBy = recordedBy.ToString()
        };

        return Result<RecoveryTransaction>.Success(transaction);
    }
}
