namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed record WriteOffBreakdownDto(
    string ReasonCode,
    decimal Amount,
    int Count);
