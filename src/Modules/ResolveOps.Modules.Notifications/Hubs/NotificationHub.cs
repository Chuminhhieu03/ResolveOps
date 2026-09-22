using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ResolveOps.Observability;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user != null)
        {
            var userId = user.TryGetUserId();
            var tenantId = user.TryGetTenantId();

            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            }

            if (tenantId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");
            }

            NotificationMetrics.RealtimeActiveConnections.Add(1);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        NotificationMetrics.RealtimeActiveConnections.Add(-1);
        await base.OnDisconnectedAsync(exception);
    }
}
