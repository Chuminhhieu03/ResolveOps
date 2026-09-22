using System;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public record RecordDecisionRequest(
    string Decision,
    decimal? ApprovedAmount,
    string[]? ReasonCodes,
    string? CarrierReference,
    string? Notes,
    DateTimeOffset ResponseAtUtc,
    string SourceChannel);
