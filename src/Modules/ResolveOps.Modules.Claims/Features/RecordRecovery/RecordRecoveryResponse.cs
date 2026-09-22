using System;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public record RecordRecoveryResponse(
    Guid ClaimId,
    Guid RecoveryTransactionId,
    string Status,
    string TransactionType,
    string ExternalReference,
    decimal Amount,
    string Currency,
    decimal TotalRecoveredAmount,
    decimal ApprovedAmount,
    decimal RemainingBalance,
    DateTimeOffset ReceivedAtUtc);
