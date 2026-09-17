namespace ResolveOps.Modules.Exceptions.Features.Cases.CreateManualCase;

public sealed record CreateManualCaseCommand(
    Guid ShipmentId,
    Guid? ShipmentLegId,
    string ExceptionType,
    string Reason,
    string? Severity = null,
    decimal? FinancialExposure = null,
    string? ExposureCurrency = null,
    string? OwnerTeamCode = null);

public sealed record CreateManualCaseResponse(
    Guid Id,
    string CaseNumber,
    Guid ShipmentId,
    string ExceptionType,
    string Status,
    string Severity,
    string? OwnerTeamCode,
    decimal FinancialExposure,
    string ExposureCurrency,
    DateTimeOffset DetectedAtUtc,
    string ConcurrencyStamp);
