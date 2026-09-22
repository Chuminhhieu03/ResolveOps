using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Claims;

public static class RecoveryTransactionType
{
    public const string Payment = "Payment";
    public const string CreditNote = "CreditNote";
    public const string Adjustment = "Adjustment";

    public static readonly IReadOnlyList<string> All =
    [
        Payment,
        CreditNote,
        Adjustment
    ];

    public static bool IsValid(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(type);
}
