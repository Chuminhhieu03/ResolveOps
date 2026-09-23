using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetOperationsDashboard;

public sealed record GetOperationsDashboardResponse(
    int TotalOpenCases,
    IReadOnlyDictionary<string, int> OpenCasesBySeverity,
    int SlaBreachedCount,
    int SlaAtRiskCount,
    int UnassignedTasksCount,
    double ActiveCarrierDelayRate,
    int TodayTrackingEventsCount,
    int TodayExceptionsDetectedCount,
    DateTimeOffset GeneratedAtUtc);
