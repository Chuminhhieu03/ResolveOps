using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed record GetFinancialRecoveryReportQuery(
    Guid TenantId,
    Guid? CarrierId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
