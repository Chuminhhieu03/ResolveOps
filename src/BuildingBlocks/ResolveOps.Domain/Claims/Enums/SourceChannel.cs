using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class SourceChannel
{
    public const string Email = "Email";
    public const string Portal = "Portal";
    public const string EDI = "EDI";
    public const string Api = "Api";
    public const string Manual = "Manual";

    public static readonly IReadOnlyList<string> All =
    [
        Email,
        Portal,
        EDI,
        Api,
        Manual
    ];
}
