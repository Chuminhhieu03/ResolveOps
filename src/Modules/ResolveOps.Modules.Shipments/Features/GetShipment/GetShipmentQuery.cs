namespace ResolveOps.Modules.Shipments.Features.GetShipment;

public sealed record GetShipmentQuery(Guid ShipmentId);

public sealed record ShipmentDetailResponse(
    Guid Id,
    string ExternalReference,
    string SourceSystem,
    Guid CustomerId,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    string Status,
    string? ServiceLevel,
    DateTimeOffset PlannedPickupAtUtc,
    DateTimeOffset PlannedDeliveryAtUtc,
    DateTimeOffset? ActualPickupAtUtc,
    DateTimeOffset? ActualDeliveryAtUtc,
    decimal? DeclaredValue,
    string? DeclaredValueCurrency,
    int? ExpectedPackageCount,
    decimal? ExpectedWeight,
    string? WeightUnit,
    IReadOnlyList<ShipmentLegResponse> Legs,
    IReadOnlyList<ShipmentItemResponse> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string ConcurrencyStamp);

public sealed record ShipmentLegResponse(
    Guid Id,
    int SequenceNumber,
    Guid CarrierId,
    string? TrackingNumber,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    DateTimeOffset? ActualDepartureAtUtc,
    DateTimeOffset? ActualArrivalAtUtc,
    string Status);

public sealed record ShipmentItemResponse(
    Guid Id,
    string LineReference,
    string? Sku,
    string? Description,
    decimal ExpectedQuantity,
    string QuantityUnit,
    decimal? UnitValue,
    string? Currency);
