using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed record CarrierSlaPerformanceDto(
    Guid? CarrierId,
    string CarrierName,
    int TotalClocks,
    int MetCount,
    int BreachedCount,
    double CompliancePercentage);
