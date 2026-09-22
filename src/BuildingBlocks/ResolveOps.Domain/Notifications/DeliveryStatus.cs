using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Notifications;

public static class DeliveryStatus
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Retrying = "Retrying";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        Sent,
        Failed,
        Retrying
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status);
}
