using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

public static class NotificationMetrics
{
    public const string MeterName = "ResolveOps.Notifications";
    public static readonly Meter Meter = new(MeterName);

    public static readonly ActivitySource ActivitySource = new(MeterName);

    public static readonly Counter<long> NotificationsSentTotal = Meter.CreateCounter<long>(
        "notifications.sent.total",
        description: "Total number of notifications successfully sent");

    public static readonly Counter<long> NotificationsFailedTotal = Meter.CreateCounter<long>(
        "notifications.failures.total",
        description: "Total number of notification delivery failures");

    public static readonly Histogram<double> EmailDurationSeconds = Meter.CreateHistogram<double>(
        "notifications.email.duration.seconds",
        unit: "s",
        description: "Duration of email dispatch operations");

    public static readonly UpDownCounter<long> RealtimeActiveConnections = Meter.CreateUpDownCounter<long>(
        "notifications.realtime.active_connections",
        description: "Number of active realtime SignalR connections");
}
