using System;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public record RecordAcknowledgementRequest(
    string? CarrierReference,
    DateTimeOffset ResponseAtUtc,
    string? Notes,
    string SourceChannel);
