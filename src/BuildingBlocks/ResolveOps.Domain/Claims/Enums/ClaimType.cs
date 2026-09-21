using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class ClaimType
{
    public const string CargoDamage = "CargoDamage";
    public const string TotalLoss = "TotalLoss";
    public const string Shortage = "Shortage";
    public const string Delay = "Delay";

    public static readonly IReadOnlyList<string> All =
    [
        CargoDamage,
        TotalLoss,
        Shortage,
        Delay
    ];
}
