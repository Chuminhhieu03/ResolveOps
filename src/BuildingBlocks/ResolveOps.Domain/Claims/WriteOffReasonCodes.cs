using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Claims;

public static class WriteOffReasonCodes
{
    public const string CarrierInsolvent = "CarrierInsolvent";
    public const string DisputedUnrecoverable = "DisputedUnrecoverable";
    public const string DeMinimisBalance = "DeMinimisBalance";
    public const string CommercialSettlement = "CommercialSettlement";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        CarrierInsolvent,
        DisputedUnrecoverable,
        DeMinimisBalance,
        CommercialSettlement,
        Other
    ];

    public static bool IsValid(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && All.Contains(reason);
}
