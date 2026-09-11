namespace ResolveOps.Modules.Tracking.Features.ManualIngest;

public sealed record ManualIngestCommand(
    Guid CarrierId,
    string TrackingNumber,
    string EventType,
    string? EventCode,
    DateTimeOffset OccurredAtUtc,
    string? LocationText,
    int? PackageCount,
    decimal? Quantity,
    string? Notes);

public sealed record ManualIngestResponse(
    Guid ReceiptId,
    string Status,
    string Message);
