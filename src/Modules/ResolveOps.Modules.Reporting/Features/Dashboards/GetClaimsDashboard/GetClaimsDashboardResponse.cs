using System;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetClaimsDashboard;

public sealed record GetClaimsDashboardResponse(
    int TotalClaims,
    int DraftClaimsCount,
    int UnderReviewClaimsCount,
    int SubmittedClaimsCount,
    int DisputedClaimsCount,
    decimal TotalClaimedAmount,
    decimal TotalApprovedAmount,
    decimal TotalRecoveredAmount,
    double RecoveryRatePercentage,
    double AvgTimeToCarrierDecisionHours,
    DateTimeOffset GeneratedAtUtc);
