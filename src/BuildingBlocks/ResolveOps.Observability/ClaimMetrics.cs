using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

public static class ClaimMetrics
{
    public const string MeterName = "ResolveOps.Claims";
    public static readonly Meter Meter = new(MeterName);

    public static readonly ActivitySource ActivitySource = new(MeterName);

    public static readonly Counter<long> ClaimsDraftedTotal = Meter.CreateCounter<long>(
        "claims.drafted.total",
        description: "Total number of drafted claims");

    public static readonly Counter<long> ClaimsEligibilityEvaluatedTotal = Meter.CreateCounter<long>(
        "claims.eligibility.evaluated.total",
        description: "Total number of claim eligibility evaluations");

    public static readonly Counter<double> ClaimsAmountClaimedTotal = Meter.CreateCounter<double>(
        "claims.amount.claimed.total",
        description: "Total claimed amount in base currency");

    public static readonly Counter<long> ClaimsApprovedTotal = Meter.CreateCounter<long>(
        "claims.approved.total",
        description: "Total number of claims approved for submission");

    public static readonly Counter<long> ClaimsSubmittedTotal = Meter.CreateCounter<long>(
        "claims.submitted.total",
        description: "Total number of claims submitted to carriers");

    public static readonly Counter<long> ClaimsDecisionsRecordedTotal = Meter.CreateCounter<long>(
        "claims.decisions.recorded.total",
        description: "Total number of carrier claim decisions recorded");

    public static readonly Counter<long> ClaimsAppealedTotal = Meter.CreateCounter<long>(
        "claims.appealed.total",
        description: "Total number of claims appealed");

    public static readonly Counter<double> ClaimsAmountApprovedTotal = Meter.CreateCounter<double>(
        "claims.amount.approved.total",
        description: "Total approved claim amount across carriers");
}
