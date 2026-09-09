namespace ResolveOps.Modules.Partners.Features.ListCarriers;

public sealed record ListCarriersQuery(
    string? StatusFilter,
    int Page,
    int PageSize);
