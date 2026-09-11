namespace ResolveOps.Modules.Integrations.Features.Quarantine;

public sealed record QuarantinedEventSummaryDto(
    Guid Id,
    Guid InboundReceiptId,
    string ReasonCode,
    string Detail,
    string Status,
    Guid? AssignedUserId,
    Guid? ResolvedShipmentId,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record QuarantinedEventDetailDto(
    Guid Id,
    Guid InboundReceiptId,
    string ReasonCode,
    string Detail,
    string Status,
    Guid? AssignedUserId,
    Guid? ResolvedShipmentId,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string SourceSystem,
    string? ExternalEventId,
    string RawPayload,
    DateTimeOffset ReceivedAtUtc);

public sealed record ListQuarantinedEventsResponse(
    IReadOnlyList<QuarantinedEventSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ResolveQuarantinedEventRequest(
    Guid ShipmentId);
