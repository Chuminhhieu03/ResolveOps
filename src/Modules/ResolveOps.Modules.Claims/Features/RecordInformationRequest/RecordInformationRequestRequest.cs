using System;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public record RecordInformationRequestRequest(
    string? CarrierReference,
    DateTimeOffset ResponseAtUtc,
    string? Notes,
    string[] ReasonCodes,
    DateTimeOffset InfoDeadlineAtUtc,
    string SourceChannel);
