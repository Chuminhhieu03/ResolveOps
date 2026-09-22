using System.Collections.Generic;

namespace ResolveOps.Modules.Notifications.Features.GetNotifications;

public sealed record GetNotificationsResponse(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
