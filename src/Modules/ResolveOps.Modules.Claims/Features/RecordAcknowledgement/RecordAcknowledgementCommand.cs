using System;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public record RecordAcknowledgementCommand(
    Guid ClaimId,
    string? CarrierReference,
    DateTimeOffset ResponseAtUtc,
    string? Notes,
    Guid RecordedBy,
    string SourceChannel);
