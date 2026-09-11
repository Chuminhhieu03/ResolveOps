namespace ResolveOps.Domain.Shipments;

/// <summary>
/// Shipment aggregate root (spec §15.5).
///
/// Invariants:
/// - (tenant_id, source_system, external_reference) is unique (database UNIQUE INDEX).
/// - A shipment can only be cancelled when its status is Active or InTransit.
/// - ConcurrencyStamp is refreshed on every mutation for optimistic concurrency (ADR-006).
/// - Legs, items, and tracking aliases are created through this aggregate only.
/// - Declared value uses decimal to avoid floating-point rounding (spec §0.1 rule 12).
/// </summary>
public sealed class Shipment : IAuditableEntity, IHasConcurrencyStamp
{
    private readonly List<ShipmentLeg> _legs = [];
    private readonly List<ShipmentItem> _items = [];
    private readonly List<ShipmentTrackingAlias> _trackingAliases = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>External reference from the source system, e.g. order number.</summary>
    public string ExternalReference { get; private set; } = string.Empty;

    /// <summary>Source system identifier, e.g. "ERP", "MANUAL", "TMS".</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }
    public Guid OriginLocationId { get; private set; }
    public Guid DestinationLocationId { get; private set; }

    public string Status { get; private set; } = ShipmentStatus.Active;

    /// <summary>Service level, e.g. "Standard", "Express". Optional.</summary>
    public string? ServiceLevel { get; private set; }

    public DateTimeOffset PlannedPickupAtUtc { get; private set; }
    public DateTimeOffset PlannedDeliveryAtUtc { get; private set; }
    public DateTimeOffset? ActualPickupAtUtc { get; private set; }
    public DateTimeOffset? ActualDeliveryAtUtc { get; private set; }

    /// <summary>Declared cargo value. Use decimal — never float/double (spec §0.1 rule 12).</summary>
    public decimal? DeclaredValue { get; private set; }

    /// <summary>ISO 4217 currency code. Required when DeclaredValue is present.</summary>
    public string? DeclaredValueCurrency { get; private set; }

    public int? ExpectedPackageCount { get; private set; }
    public decimal? ExpectedWeight { get; private set; }
    public string? WeightUnit { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Optimistic concurrency token (ADR-006).</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // Navigation properties — EF Core populates these.
    public IReadOnlyList<ShipmentLeg> Legs => _legs.AsReadOnly();
    public IReadOnlyList<ShipmentItem> Items => _items.AsReadOnly();
    public IReadOnlyList<ShipmentTrackingAlias> TrackingAliases => _trackingAliases.AsReadOnly();

    // EF Core requires a parameterless constructor.
    private Shipment() { }

    /// <summary>
    /// Creates a new shipment with optional legs and items.
    /// </summary>
    public static Shipment Create(
        Guid tenantId,
        string externalReference,
        string sourceSystem,
        Guid customerId,
        Guid originLocationId,
        Guid destinationLocationId,
        DateTimeOffset plannedPickupAtUtc,
        DateTimeOffset plannedDeliveryAtUtc,
        string? serviceLevel,
        decimal? declaredValue,
        string? declaredValueCurrency,
        int? expectedPackageCount,
        decimal? expectedWeight,
        string? weightUnit,
        IEnumerable<LegDefinition> legs,
        IEnumerable<ItemDefinition> items,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var shipment = new Shipment
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ExternalReference = externalReference.Trim(),
            SourceSystem = sourceSystem.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            OriginLocationId = originLocationId,
            DestinationLocationId = destinationLocationId,
            Status = ShipmentStatus.Active,
            ServiceLevel = serviceLevel?.Trim(),
            PlannedPickupAtUtc = plannedPickupAtUtc,
            PlannedDeliveryAtUtc = plannedDeliveryAtUtc,
            DeclaredValue = declaredValue,
            DeclaredValueCurrency = declaredValueCurrency?.Trim().ToUpperInvariant(),
            ExpectedPackageCount = expectedPackageCount,
            ExpectedWeight = expectedWeight,
            WeightUnit = weightUnit?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };

        foreach (var leg in legs)
        {
            var legEntity = ShipmentLeg.Create(
                tenantId,
                shipment.Id,
                leg.SequenceNumber,
                leg.CarrierId,
                leg.TrackingNumber,
                leg.OriginLocationId,
                leg.DestinationLocationId,
                leg.PlannedDepartureAtUtc,
                leg.PlannedArrivalAtUtc);

            shipment._legs.Add(legEntity);

            // Auto-create tracking alias when a tracking number is supplied on a leg.
            if (!string.IsNullOrWhiteSpace(leg.TrackingNumber))
            {
                var alias = ShipmentTrackingAlias.Create(
                    tenantId,
                    shipment.Id,
                    legEntity.Id,
                    leg.CarrierId,
                    TrackingAliasType.TrackingNumber,
                    leg.TrackingNumber);

                shipment._trackingAliases.Add(alias);
            }
        }

        foreach (var item in items)
        {
            shipment._items.Add(ShipmentItem.Create(
                tenantId,
                shipment.Id,
                item.LineReference,
                item.Sku,
                item.Description,
                item.ExpectedQuantity,
                item.QuantityUnit,
                item.UnitValue,
                item.Currency));
        }

        return shipment;
    }

    /// <summary>
    /// Cancels the shipment.
    /// Only Active or InTransit shipments can be cancelled (spec §24 Phase 4 DoD).
    /// </summary>
    public Result Cancel(TimeProvider timeProvider)
    {
        if (!ShipmentStatus.CanCancel(Status))
        {
            return DomainError.Failure(
                "ERR_SHIPMENT_CANNOT_CANCEL",
                $"A shipment with status '{Status}' cannot be cancelled.");
        }

        Status = ShipmentStatus.Cancelled;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        ConcurrencyStamp = Guid.NewGuid().ToString("N");

        return Result.Success();
    }

    /// <summary>
    /// Projects actual milestones and shipment status from a normalized tracking event (spec §24 Phase 6, §26.3).
    ///
    /// Invariants:
    /// - Status will never regress from Delivered to InTransit due to late arriving out-of-order events.
    /// - PickedUp sets ActualPickupAtUtc (preserving earliest timestamp) and transitions Active to InTransit.
    /// - InTransit / OutForDelivery transitions Active to InTransit.
    /// - Delivered sets ActualDeliveryAtUtc and transitions to Delivered.
    /// - ConcurrencyStamp is refreshed on every projection update.
    /// </summary>
    public void ApplyTrackingEvent(string eventType, DateTimeOffset occurredAtUtc, TimeProvider timeProvider)
    {
        if (string.Equals(eventType, "PickedUp", StringComparison.OrdinalIgnoreCase))
        {
            if (ActualPickupAtUtc is null || occurredAtUtc < ActualPickupAtUtc.Value)
            {
                ActualPickupAtUtc = occurredAtUtc;
            }

            if (Status == ShipmentStatus.Active)
            {
                Status = ShipmentStatus.InTransit;
            }
        }
        else if (string.Equals(eventType, "InTransit", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(eventType, "OutForDelivery", StringComparison.OrdinalIgnoreCase))
        {
            if (Status == ShipmentStatus.Active)
            {
                Status = ShipmentStatus.InTransit;
            }
            // Out-of-order safety: if already Delivered, status does NOT regress.
        }
        else if (string.Equals(eventType, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            if (ActualDeliveryAtUtc is null || occurredAtUtc > ActualDeliveryAtUtc.Value)
            {
                ActualDeliveryAtUtc = occurredAtUtc;
            }

            if (Status != ShipmentStatus.Cancelled)
            {
                Status = ShipmentStatus.Delivered;
            }
        }

        UpdatedAtUtc = timeProvider.GetUtcNow();
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}

/// <summary>Defines a shipment leg to be created with the shipment.</summary>
public sealed record LegDefinition(
    int SequenceNumber,
    Guid CarrierId,
    string? TrackingNumber,
    Guid OriginLocationId,
    Guid DestinationLocationId,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc);

/// <summary>Defines a shipment item to be created with the shipment.</summary>
public sealed record ItemDefinition(
    string LineReference,
    string? Sku,
    string? Description,
    decimal ExpectedQuantity,
    string QuantityUnit,
    decimal? UnitValue,
    string? Currency);
