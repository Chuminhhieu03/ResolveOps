using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class CarrierResponseType
{
    public const string Acknowledged = "Acknowledged";
    public const string MoreInformationRequested = "MoreInformationRequested";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Denied = "Denied";
    public const string SettlementOffered = "SettlementOffered";

    public static readonly IReadOnlyList<string> All =
    [
        Acknowledged,
        MoreInformationRequested,
        Approved,
        PartiallyApproved,
        Denied,
        SettlementOffered
    ];
}
