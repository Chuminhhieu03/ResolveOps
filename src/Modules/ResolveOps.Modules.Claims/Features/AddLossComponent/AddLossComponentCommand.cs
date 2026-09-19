using System;

namespace ResolveOps.Modules.Claims.Features.AddLossComponent;

public record AddLossComponentCommand(
    Guid ClaimId,
    string ComponentType,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
