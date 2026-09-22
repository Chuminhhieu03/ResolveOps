using System;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public record RecordAcknowledgementResponse(
    Guid ClaimId,
    string Status,
    Guid ResponseId,
    DateTimeOffset ResponseAtUtc);
