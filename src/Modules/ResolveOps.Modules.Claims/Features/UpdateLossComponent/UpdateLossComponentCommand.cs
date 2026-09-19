using System;

namespace ResolveOps.Modules.Claims.Features.UpdateLossComponent;

public record UpdateLossComponentCommand(
    Guid ClaimId,
    Guid ComponentId,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
