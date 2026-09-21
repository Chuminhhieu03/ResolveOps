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
}
