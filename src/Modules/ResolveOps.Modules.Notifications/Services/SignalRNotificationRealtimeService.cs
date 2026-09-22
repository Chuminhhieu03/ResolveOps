using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ResolveOps.Application.Notifications;
using ResolveOps.Modules.Notifications.Hubs;

namespace ResolveOps.Modules.Notifications.Services;

public sealed class SignalRNotificationRealtimeService : INotificationRealtimeService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimeService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendNotificationToUserAsync(Guid tenantId, Guid userId, object notificationPayload, CancellationToken ct = default)
    {
        return _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notificationPayload, ct);
    }

    public Task BroadcastToTenantAsync(Guid tenantId, string eventType, object eventPayload, CancellationToken ct = default)
    {
        return _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("ReceiveEvent", new
        {
            EventType = eventType,
            Payload = eventPayload,
            TimestampUtc = DateTimeOffset.UtcNow
        }, ct);
    }
}
