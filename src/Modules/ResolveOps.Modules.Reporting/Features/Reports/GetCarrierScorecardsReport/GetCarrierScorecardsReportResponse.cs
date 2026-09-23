using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetCarrierScorecardsReport;

public sealed record GetCarrierScorecardsReportResponse(
    IReadOnlyList<CarrierScorecardDto> Scorecards,
    DateTimeOffset GeneratedAtUtc);
