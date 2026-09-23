using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed record GetSlaPerformanceReportResponse(
    int TotalClocksTracked,
    int MetCount,
    int BreachedCount,
    double OverallCompliancePercentage,
    double AvgTimeToTriageHours,
    double AvgTimeToResolutionHours,
    IReadOnlyList<CarrierSlaPerformanceDto> CarrierBreakdown,
    DateTimeOffset GeneratedAtUtc);
