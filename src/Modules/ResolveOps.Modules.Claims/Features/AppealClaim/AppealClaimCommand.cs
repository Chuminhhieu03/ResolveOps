using System;

namespace ResolveOps.Modules.Claims.Features.AppealClaim;

public record AppealClaimCommand(Guid ClaimId, string AppealReason, string? Notes, Guid AppealedBy);
