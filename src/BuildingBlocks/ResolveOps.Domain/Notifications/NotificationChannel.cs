using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Notifications;

public static class NotificationChannel
{
    public const string InApp = "InApp";
    public const string Email = "Email";

    public static readonly IReadOnlyList<string> All =
    [
        InApp,
        Email
    ];

    public static bool IsValid(string? channel) =>
        !string.IsNullOrWhiteSpace(channel) && All.Contains(channel);
}
