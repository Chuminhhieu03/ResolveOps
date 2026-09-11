namespace ResolveOps.Domain.Tracking;

/// <summary>
/// Canonical tracking event types (spec §15.6, §24 Phase 6).
/// </summary>
public static class TrackingEventType
{
    public const string PickedUp = "PickedUp";
    public const string InTransit = "InTransit";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string Exception = "Exception";
    public const string Delay = "Delay";
    public const string Damaged = "Damaged";

    public static readonly IReadOnlyList<string> All =
    [
        PickedUp,
        InTransit,
        OutForDelivery,
        Delivered,
        Exception,
        Delay,
        Damaged,
    ];
}

/// <summary>
/// Processing status of an inbound event receipt (spec §15.6).
/// </summary>
public static class InboundReceiptStatus
{
    public const string Received = "Received";
    public const string Normalized = "Normalized";
    public const string Quarantined = "Quarantined";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All = [Received, Normalized, Quarantined, Failed];
}

/// <summary>
/// Status of a quarantined tracking event (spec §15.6).
/// </summary>
public static class QuarantinedEventStatus
{
    public const string Quarantined = "Quarantined";
    public const string Resolved = "Resolved";
    public const string Reprocessed = "Reprocessed";
    public const string Ignored = "Ignored";

    public static readonly IReadOnlyList<string> All = [Quarantined, Resolved, Reprocessed, Ignored];
}

/// <summary>
/// Quarantine reason codes (spec §15.6, §25.3).
/// </summary>
public static class QuarantineReasonCodes
{
    public const string UnmatchedShipment = "UNMATCHED_SHIPMENT";
    public const string InvalidPayload = "INVALID_PAYLOAD";
    public const string UnknownCarrier = "UNKNOWN_CARRIER";
    public const string UnknownEventType = "UNKNOWN_EVENT_TYPE";
    public const string StaleOrCorrupt = "STALE_OR_CORRUPT";
}
