using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetExceptionAgeingReport;

public sealed record GetExceptionAgeingReportQuery(
    Guid TenantId,
    Guid? CarrierId = null,
    string? Severity = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
