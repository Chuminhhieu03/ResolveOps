using System;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public record RecordDecisionResponse(
    Guid ClaimId,
    string Status,
    decimal ApprovedAmount,
    decimal DeniedDifference,
    decimal ClaimedAmount,
    Guid ResponseId,
    DateTimeOffset ResponseAtUtc);
