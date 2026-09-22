using System;

namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public record WriteOffClaimCommand(
    Guid ClaimId,
    decimal WriteOffAmount,
    string Reason,
    Guid ApprovedBy);
