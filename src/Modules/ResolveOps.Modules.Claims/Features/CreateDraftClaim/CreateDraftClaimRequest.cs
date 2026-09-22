using System;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public record CreateDraftClaimRequest(
    Guid CarrierId,
    string ClaimType,
    string Currency);
