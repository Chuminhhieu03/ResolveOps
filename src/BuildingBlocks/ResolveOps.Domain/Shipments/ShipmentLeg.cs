namespace ResolveOps.Domain.Shipments;

/// <summary>
/// Shipment leg — one carrier-operated segment of a multi-leg shipment (spec §15.5).
///
/// Invariants:
/// - SequenceNumber is unique within a shipment (enforced by database unique index).
/// - An inactive carrier check is applied at the command level before creating a leg.
/// </summary>
public sealed class ShipmentLeg : IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ShipmentId { get; private set; }
    public int SequenceNumber { get; private set; }

    /// <summary>FK to carriers table.</summary>
    public Guid CarrierId { get; private set; }

    public string? TrackingNumber { get; private set; }

    public Guid OriginLocationId { get; private set; }
    public Guid DestinationLocationId { get; private set; }

    public DateTimeOffset? PlannedDepartureAtUtc { get; private set; }
    public DateTimeOffset? PlannedArrivalAtUtc { get; private set; }
    public DateTimeOffset? ActualDepartureAtUtc { get; private set; }
    public DateTimeOffset? ActualArrivalAtUtc { get; private set; }

    public string Status { get; private set; } = ShipmentLegStatus.Pending;

    /// <summary>Optimistic concurrency token (spec §15.1 / ADR-006).</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // EF Core requires a parameterless constructor.
    private ShipmentLeg() { }

    internal static ShipmentLeg Create(
        Guid tenantId,
        Guid shipmentId,
        int sequenceNumber,
        Guid carrierId,
        string? trackingNumber,
        Guid originLocationId,
        Guid destinationLocationId,
        DateTimeOffset? plannedDepartureAtUtc,
        DateTimeOffset? plannedArrivalAtUtc)
    {
        return new ShipmentLeg
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ShipmentId = shipmentId,
            SequenceNumber = sequenceNumber,
            CarrierId = carrierId,
            TrackingNumber = trackingNumber?.Trim(),
            OriginLocationId = originLocationId,
            DestinationLocationId = destinationLocationId,
            PlannedDepartureAtUtc = plannedDepartureAtUtc,
            PlannedArrivalAtUtc = plannedArrivalAtUtc,
            Status = ShipmentLegStatus.Pending,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }
}
