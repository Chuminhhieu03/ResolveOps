using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResolveOps.Application.Notifications;

public interface INotificationRealtimeService
{
    Task SendNotificationToUserAsync(Guid tenantId, Guid userId, object notificationPayload, CancellationToken ct = default);
    Task BroadcastToTenantAsync(Guid tenantId, string eventType, object eventPayload, CancellationToken ct = default);
}
