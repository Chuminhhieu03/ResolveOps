namespace ResolveOps.Modules.Shipments.Features.CreateShipment;

public sealed record CreateShipmentCommand(
    string ExternalReference,
    string SourceSystem,
    Guid CustomerId,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    DateTimeOffset PlannedPickupAt,
    DateTimeOffset PlannedDeliveryAt,
    string? ServiceLevel,
    decimal? DeclaredValue,
    string? DeclaredValueCurrency,
    int? ExpectedPackageCount,
    decimal? ExpectedWeight,
    string? WeightUnit,
    IReadOnlyList<CreateLegRequest> Legs,
    IReadOnlyList<CreateItemRequest> Items);

public sealed record CreateLegRequest(
    int SequenceNumber,
    Guid CarrierId,
    string? TrackingNumber,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    DateTimeOffset? PlannedDepartureAt,
    DateTimeOffset? PlannedArrivalAt);

public sealed record CreateItemRequest(
    string LineReference,
    string? Sku,
    string? Description,
    decimal ExpectedQuantity,
    string QuantityUnit,
    decimal? UnitValue,
    string? Currency);
