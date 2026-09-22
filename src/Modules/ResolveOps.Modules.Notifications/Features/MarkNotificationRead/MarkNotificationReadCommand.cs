using System;

namespace ResolveOps.Modules.Notifications.Features.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid UserId);
