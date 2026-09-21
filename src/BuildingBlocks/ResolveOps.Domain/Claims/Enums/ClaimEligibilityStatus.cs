using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class ClaimEligibilityStatus
{
    public const string Pending = "Pending";
    public const string Eligible = "Eligible";
    public const string NotEligible = "NotEligible";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        Eligible,
        NotEligible
    ];
}
