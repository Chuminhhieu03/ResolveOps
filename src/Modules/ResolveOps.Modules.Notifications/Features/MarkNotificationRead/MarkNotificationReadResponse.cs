using System;

namespace ResolveOps.Modules.Notifications.Features.MarkNotificationRead;

public sealed record MarkNotificationReadResponse(
    Guid NotificationId,
    bool IsRead,
    DateTimeOffset? ReadAtUtc);
