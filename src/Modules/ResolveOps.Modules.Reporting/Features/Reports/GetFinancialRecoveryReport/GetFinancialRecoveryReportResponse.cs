using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed record GetFinancialRecoveryReportResponse(
    decimal TotalApproved,
    decimal TotalRecovered,
    decimal TotalWrittenOff,
    decimal PendingRecoveryBalance,
    IReadOnlyList<RecoveryTypeBreakdownDto> RecoveryTypeBreakdown,
    IReadOnlyList<CarrierRecoveryBreakdownDto> CarrierBreakdown,
    IReadOnlyList<WriteOffBreakdownDto> WriteOffBreakdown,
    DateTimeOffset GeneratedAtUtc);
