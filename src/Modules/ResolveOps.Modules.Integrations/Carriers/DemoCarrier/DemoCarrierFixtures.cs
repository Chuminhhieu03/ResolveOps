namespace ResolveOps.Modules.Integrations.Carriers.DemoCarrier;

/// <summary>
/// Documented fixture payloads for DemoCarrier webhook integration (spec §24 Phase 6 task 2, §26.2, §27.4).
/// </summary>
public static class DemoCarrierFixtures
{
    public const string PickupEventJson = """
    {
      "eventId": "EVT-PU-1001",
      "trackingNumber": "VN123456789",
      "statusCode": "PU",
      "statusDescription": "Shipment picked up at origin distribution center",
      "timestamp": "2026-08-10T08:30:00Z",
      "location": "Hanoi Distribution Center",
      "packageCount": 24,
      "weight": 125.5,
      "weightUnit": "KG",
      "exceptionReason": null
    }
    """;

    public const string InTransitEventJson = """
    {
      "eventId": "EVT-IT-1002",
      "trackingNumber": "VN123456789",
      "statusCode": "IT",
      "statusDescription": "In transit to destination facility",
      "timestamp": "2026-08-10T11:15:00Z",
      "location": "Noi Bai Transit Hub",
      "packageCount": 24,
      "weight": 125.5,
      "weightUnit": "KG",
      "exceptionReason": null
    }
    """;

    public const string OutForDeliveryEventJson = """
    {
      "eventId": "EVT-OFD-1003",
      "trackingNumber": "VN123456789",
      "statusCode": "OFD",
      "statusDescription": "Out for delivery with courier",
      "timestamp": "2026-08-10T13:45:00Z",
      "location": "Bac Ninh Hub",
      "packageCount": 24,
      "weight": 125.5,
      "weightUnit": "KG",
      "exceptionReason": null
    }
    """;

    public const string DeliveredEventJson = """
    {
      "eventId": "EVT-DEL-1004",
      "trackingNumber": "VN123456789",
      "statusCode": "DEL",
      "statusDescription": "Delivered and signed by consignee",
      "timestamp": "2026-08-10T15:00:00Z",
      "location": "Bac Ninh Manufacturing Plant",
      "packageCount": 24,
      "weight": 125.5,
      "weightUnit": "KG",
      "exceptionReason": null
    }
    """;

    public const string DamageEventJson = """
    {
      "eventId": "EVT-DMG-1005",
      "trackingNumber": "VN123456789",
      "statusCode": "DMG",
      "statusDescription": "Cargo damage reported during transit",
      "timestamp": "2026-08-10T10:00:00Z",
      "location": "Noi Bai Transit Hub",
      "packageCount": 24,
      "weight": 125.5,
      "weightUnit": "KG",
      "exceptionReason": "Three wet cartons and one leaning pallet observed."
    }
    """;

    public const string UnmatchedShipmentEventJson = """
    {
      "eventId": "EVT-UNMATCHED-9999",
      "trackingNumber": "UNKNOWN-TRACKING-999999",
      "statusCode": "IT",
      "statusDescription": "Package in transit",
      "timestamp": "2026-08-10T09:00:00Z",
      "location": "Unknown Location",
      "packageCount": 1,
      "weight": 5.0,
      "weightUnit": "KG",
      "exceptionReason": null
    }
    """;
}
