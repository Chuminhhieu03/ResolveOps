using System;

namespace ResolveOps.Modules.Claims.Features.CloseClaim;

public record CloseClaimResponse(
    Guid ClaimId,
    string Status,
    decimal ClaimedAmount,
    decimal ApprovedAmount,
    decimal RecoveredAmount,
    decimal WrittenOffAmount,
    DateTimeOffset ClosedAtUtc,
    Guid ClosedBy,
    string? ClosingNotes);
