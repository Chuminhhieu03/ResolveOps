namespace ResolveOps.Modules.Shipments.Features.ListShipments;

public sealed record ListShipmentsQuery(
    int Page = 1,
    int PageSize = 25,
    string? StatusFilter = null);

public sealed record ListShipmentsResponse(
    IReadOnlyList<ShipmentSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ShipmentSummaryResponse(
    Guid Id,
    string ExternalReference,
    string SourceSystem,
    Guid CustomerId,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    string Status,
    DateTimeOffset PlannedPickupAtUtc,
    DateTimeOffset PlannedDeliveryAtUtc,
    DateTimeOffset? ActualDeliveryAtUtc,
    int LegCount,
    DateTimeOffset CreatedAtUtc,
    string ConcurrencyStamp);
