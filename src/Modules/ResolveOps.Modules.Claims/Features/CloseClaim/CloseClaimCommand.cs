using System;

namespace ResolveOps.Modules.Claims.Features.CloseClaim;

public record CloseClaimCommand(
    Guid ClaimId,
    string? ClosingNotes,
    Guid ClosedBy);
