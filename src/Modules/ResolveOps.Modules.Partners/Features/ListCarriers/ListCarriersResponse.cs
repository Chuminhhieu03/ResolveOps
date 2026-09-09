using ResolveOps.Modules.Partners.Features.GetCarrier;

namespace ResolveOps.Modules.Partners.Features.ListCarriers;

public sealed record ListCarriersResponse(
    IReadOnlyList<CarrierResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
