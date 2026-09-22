using System;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public record RecordInformationRequestResponse(
    Guid ClaimId,
    string Status,
    Guid ResponseId,
    DateTimeOffset InfoDeadlineAtUtc);
