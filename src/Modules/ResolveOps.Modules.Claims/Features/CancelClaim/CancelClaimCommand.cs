using System;

namespace ResolveOps.Modules.Claims.Features.CancelClaim;

public record CancelClaimCommand(Guid ClaimId, string Reason, Guid CancelledBy);
