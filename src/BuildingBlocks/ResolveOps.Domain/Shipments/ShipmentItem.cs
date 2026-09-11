namespace ResolveOps.Domain.Shipments;

/// <summary>
/// Shipment item — line-level cargo detail (spec §15.5).
///
/// Note: MVP may store cargo summary only. Line-level items are stored
/// when partial delivery needs precise quantity (spec §15.5 note).
/// </summary>
public sealed class ShipmentItem
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ShipmentId { get; private set; }

    /// <summary>Unique line reference within a shipment, e.g. PO line number.</summary>
    public string LineReference { get; private set; } = string.Empty;

    public string? Sku { get; private set; }
    public string? Description { get; private set; }

    public decimal ExpectedQuantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;

    public decimal? UnitValue { get; private set; }

    /// <summary>ISO 4217 currency code. Required when UnitValue is present.</summary>
    public string? Currency { get; private set; }

    // EF Core requires a parameterless constructor.
    private ShipmentItem() { }

    internal static ShipmentItem Create(
        Guid tenantId,
        Guid shipmentId,
        string lineReference,
        string? sku,
        string? description,
        decimal expectedQuantity,
        string quantityUnit,
        decimal? unitValue,
        string? currency)
    {
        return new ShipmentItem
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ShipmentId = shipmentId,
            LineReference = lineReference.Trim(),
            Sku = sku?.Trim(),
            Description = description?.Trim(),
            ExpectedQuantity = expectedQuantity,
            QuantityUnit = quantityUnit.Trim(),
            UnitValue = unitValue,
            Currency = currency?.Trim().ToUpperInvariant(),
        };
    }
}
