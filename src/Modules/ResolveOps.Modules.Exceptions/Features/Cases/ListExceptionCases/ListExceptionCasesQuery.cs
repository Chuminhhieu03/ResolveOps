namespace ResolveOps.Modules.Exceptions.Features.Cases.ListExceptionCases;

public sealed record ListExceptionCasesQuery(
    string? Status = null,
    string? Severity = null,
    string? ExceptionType = null,
    Guid? ShipmentId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ExceptionCaseSummaryResponse(
    Guid Id,
    string CaseNumber,
    Guid ShipmentId,
    Guid? ShipmentLegId,
    string ExceptionType,
    string Status,
    string Severity,
    int? SeverityScore,
    string? OwnerTeamCode,
    decimal FinancialExposure,
    string ExposureCurrency,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string ConcurrencyStamp);

public sealed record ListExceptionCasesResponse(
    IReadOnlyList<ExceptionCaseSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
