namespace ResolveOps.Domain.Shipments;

/// <summary>Valid values for <see cref="Shipment.Status"/>.</summary>
public static class ShipmentStatus
{
    public const string Active = "Active";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = [Active, InTransit, Delivered, Cancelled];

    /// <summary>
    /// Returns true when the shipment can be cancelled (spec §24 Phase 4).
    /// Only Active or InTransit shipments may be cancelled.
    /// </summary>
    public static bool CanCancel(string status) =>
        status == Active || status == InTransit;
}

/// <summary>Valid values for <see cref="ShipmentLeg.Status"/>.</summary>
public static class ShipmentLegStatus
{
    public const string Pending = "Pending";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}

/// <summary>Valid values for <see cref="ShipmentTrackingAlias.AliasType"/>.</summary>
public static class TrackingAliasType
{
    public const string TrackingNumber = "TrackingNumber";
    public const string WaybillNumber = "WaybillNumber";
    public const string BookingReference = "BookingReference";
}
