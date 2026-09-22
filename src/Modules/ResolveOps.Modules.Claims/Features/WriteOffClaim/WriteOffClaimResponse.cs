using System;

namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public record WriteOffClaimResponse(
    Guid ClaimId,
    string Status,
    decimal ClaimedAmount,
    decimal ApprovedAmount,
    decimal RecoveredAmount,
    decimal WrittenOffAmount,
    decimal RemainingExposure,
    string WriteOffReason,
    DateTimeOffset WrittenOffAtUtc,
    Guid WrittenOffBy);
