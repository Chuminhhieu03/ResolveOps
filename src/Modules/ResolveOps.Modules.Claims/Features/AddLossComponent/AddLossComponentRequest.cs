using System;

namespace ResolveOps.Modules.Claims.Features.AddLossComponent;

public record AddLossComponentRequest(
    string ComponentType,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);
