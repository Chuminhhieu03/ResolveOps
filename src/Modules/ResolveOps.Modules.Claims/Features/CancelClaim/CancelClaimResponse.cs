using System;

namespace ResolveOps.Modules.Claims.Features.CancelClaim;

public record CancelClaimResponse(
    Guid ClaimId,
    string Status,
    string Reason,
    DateTimeOffset CancelledAtUtc);
