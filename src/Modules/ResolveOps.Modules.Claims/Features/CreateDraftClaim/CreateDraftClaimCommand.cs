using System;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public record CreateDraftClaimCommand(
    Guid CaseId,
    Guid CarrierId,
    string ClaimType,
    string Currency);
