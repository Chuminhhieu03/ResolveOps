using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Claims.Features.GetClaimRecoveries;

public record RecoveryTransactionItem(
    Guid Id,
    string TransactionType,
    string ExternalReference,
    decimal Amount,
    string Currency,
    DateTimeOffset ReceivedAtUtc,
    Guid RecordedBy,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

public record GetClaimRecoveriesResponse(
    Guid ClaimId,
    string ClaimNumber,
    string Status,
    string Currency,
    decimal ClaimedAmount,
    decimal ApprovedAmount,
    decimal RecoveredAmount,
    decimal WrittenOffAmount,
    decimal OutstandingBalance,
    string? WriteOffReason,
    DateTimeOffset? WrittenOffAtUtc,
    IReadOnlyList<RecoveryTransactionItem> Transactions);
