using System;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public record RecordDecisionCommand(
    Guid ClaimId,
    string Decision,
    decimal? ApprovedAmount,
    string[]? ReasonCodes,
    string? CarrierReference,
    string? Notes,
    DateTimeOffset ResponseAtUtc,
    Guid RecordedBy,
    string SourceChannel);
