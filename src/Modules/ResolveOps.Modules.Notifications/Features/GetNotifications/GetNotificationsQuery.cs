using System;

namespace ResolveOps.Modules.Notifications.Features.GetNotifications;

public sealed record GetNotificationsQuery(
    Guid UserId,
    bool? IsRead = null,
    string? NotificationClass = null,
    int Page = 1,
    int PageSize = 20);
