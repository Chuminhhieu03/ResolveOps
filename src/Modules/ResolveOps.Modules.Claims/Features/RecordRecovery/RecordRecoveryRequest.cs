using System;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public record RecordRecoveryRequest(
    string TransactionType,
    string ExternalReference,
    decimal Amount,
    string Currency,
    DateTimeOffset ReceivedAtUtc,
    string? Notes);
