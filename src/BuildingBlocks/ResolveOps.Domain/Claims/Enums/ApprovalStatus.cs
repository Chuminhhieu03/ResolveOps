using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class ApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        Approved,
        Rejected
    ];
}
