using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetCarrierScorecardsReport;

public sealed record CarrierScorecardDto(
    Guid CarrierId,
    string CarrierName,
    int ShipmentCount,
    double ExceptionRate,
    double OnTimeRate,
    IReadOnlyDictionary<string, int> SeverityDistribution,
    double AvgResponseTimeHours,
    double ClaimApprovalRate,
    double RecoveryRate,
    decimal TotalClaimedAmount,
    decimal TotalApprovedAmount,
    decimal TotalRecoveredAmount);
