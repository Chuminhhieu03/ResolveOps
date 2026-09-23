using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed record GetSlaPerformanceReportQuery(
    Guid TenantId,
    Guid? CarrierId = null,
    string? Priority = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
