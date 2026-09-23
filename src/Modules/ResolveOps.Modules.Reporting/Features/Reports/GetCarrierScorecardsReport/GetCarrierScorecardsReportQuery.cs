using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetCarrierScorecardsReport;

public sealed record GetCarrierScorecardsReportQuery(
    Guid TenantId,
    Guid? CarrierId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
