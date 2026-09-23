namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed record RecoveryTypeBreakdownDto(
    string TransactionType,
    decimal Amount,
    int Count);
