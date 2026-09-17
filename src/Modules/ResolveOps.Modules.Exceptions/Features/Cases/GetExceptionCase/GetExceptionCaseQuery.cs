namespace ResolveOps.Modules.Exceptions.Features.Cases.GetExceptionCase;

public sealed record GetExceptionCaseQuery(Guid CaseId);

public sealed record OccurrenceDetailResponse(
    Guid Id,
    Guid? TrackingEventId,
    string OccurrenceType,
    DateTimeOffset ObservedAtUtc,
    string Summary,
    DateTimeOffset CreatedAtUtc);

public sealed record TimelineEntryDetailResponse(
    Guid Id,
    string EntryType,
    string ActorType,
    Guid? ActorId,
    string Summary,
    string? DetailsJson,
    DateTimeOffset CreatedAtUtc,
    string? CorrelationId);

public sealed record ExceptionCaseDetailResponse(
    Guid Id,
    string CaseNumber,
    Guid ShipmentId,
    Guid? ShipmentLegId,
    string ExceptionType,
    string Fingerprint,
    string Status,
    string Severity,
    int? SeverityScore,
    Guid PolicyId,
    int PolicyVersionNumber,
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    decimal FinancialExposure,
    string ExposureCurrency,
    string? RootCauseCode,
    string? DispositionCode,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string ConcurrencyStamp,
    IReadOnlyList<OccurrenceDetailResponse> Occurrences,
    IReadOnlyList<TimelineEntryDetailResponse> TimelineEntries);
