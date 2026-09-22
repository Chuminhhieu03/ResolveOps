using System;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public record RecordInformationRequestCommand(
    Guid ClaimId,
    string? CarrierReference,
    DateTimeOffset ResponseAtUtc,
    string? Notes,
    string[] ReasonCodes,
    DateTimeOffset InfoDeadlineAtUtc,
    Guid RecordedBy,
    string SourceChannel);
