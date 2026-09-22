using System;

namespace ResolveOps.Modules.Claims.Features.AppealClaim;

public record AppealClaimResponse(
    Guid ClaimId,
    string Status,
    string AppealReason,
    DateTimeOffset AppealedAtUtc);
