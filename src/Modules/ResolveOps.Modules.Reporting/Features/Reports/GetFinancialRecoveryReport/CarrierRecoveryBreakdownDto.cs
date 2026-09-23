using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed record CarrierRecoveryBreakdownDto(
    Guid CarrierId,
    string CarrierName,
    decimal TotalApproved,
    decimal TotalRecovered,
    decimal TotalWrittenOff,
    decimal PendingBalance);
