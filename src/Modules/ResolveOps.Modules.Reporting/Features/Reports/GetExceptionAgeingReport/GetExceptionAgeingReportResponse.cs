using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetExceptionAgeingReport;

public sealed record GetExceptionAgeingReportResponse(
    IReadOnlyList<ExceptionAgeingBucketDto> Items,
    int TotalOpenCases,
    DateTimeOffset GeneratedAtUtc);
