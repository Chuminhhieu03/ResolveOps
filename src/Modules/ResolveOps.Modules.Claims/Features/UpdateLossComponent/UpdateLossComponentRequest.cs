using System;

namespace ResolveOps.Modules.Claims.Features.UpdateLossComponent;

public record UpdateLossComponentRequest(
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
