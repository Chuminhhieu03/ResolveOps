namespace ResolveOps.Domain.Shipments;

/// <summary>
/// Shipment tracking alias — maps a carrier-specific identifier to a shipment leg (spec §15.5).
///
/// Used by the tracking ingestion module (Phase 6) to match inbound carrier events
/// to the correct shipment without requiring the external system to know our internal ID.
///
/// Invariants:
/// - The combination (tenant_id, carrier_id, alias_type, alias_value) is unique (UNIQUE INDEX).
/// </summary>
public sealed class ShipmentTrackingAlias
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ShipmentId { get; private set; }

    /// <summary>Null when the alias applies to the whole shipment (no specific leg).</summary>
    public Guid? ShipmentLegId { get; private set; }

    public Guid CarrierId { get; private set; }

    /// <summary>Type of alias: TrackingNumber, WaybillNumber, BookingReference.</summary>
    public string AliasType { get; private set; } = string.Empty;

    /// <summary>The actual alias value, e.g. "VN123456789".</summary>
    public string AliasValue { get; private set; } = string.Empty;

    // EF Core requires a parameterless constructor.
    private ShipmentTrackingAlias() { }

    internal static ShipmentTrackingAlias Create(
        Guid tenantId,
        Guid shipmentId,
        Guid? shipmentLegId,
        Guid carrierId,
        string aliasType,
        string aliasValue)
    {
        return new ShipmentTrackingAlias
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ShipmentId = shipmentId,
            ShipmentLegId = shipmentLegId,
            CarrierId = carrierId,
            AliasType = aliasType.Trim(),
            AliasValue = aliasValue.Trim(),
        };
    }
}
