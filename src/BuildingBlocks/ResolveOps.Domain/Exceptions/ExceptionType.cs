namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// MVP Exception types (spec §7.1, §24 Phase 7).
/// </summary>
public static class ExceptionType
{
    public const string PickupDelay = "PickupDelay";
    public const string InTransitDelay = "InTransitDelay";
    public const string Damage = "Damage";

    // Future MVP types (Phase 8+)
    public const string MissedDelivery = "MissedDelivery";
    public const string PartialDelivery = "PartialDelivery";
    public const string Loss = "Loss";
    public const string MissingOrInvalidDocument = "MissingOrInvalidDocument";

    public static readonly IReadOnlyList<string> All =
    [
        PickupDelay,
        InTransitDelay,
        Damage,
        MissedDelivery,
        PartialDelivery,
        Loss,
        MissingOrInvalidDocument
    ];

    public static bool IsValid(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
