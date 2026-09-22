using System;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public record RecordRecoveryCommand(
    Guid ClaimId,
    string TransactionType,
    string ExternalReference,
    decimal Amount,
    string Currency,
    DateTimeOffset ReceivedAtUtc,
    Guid RecordedBy,
    string? Notes);
