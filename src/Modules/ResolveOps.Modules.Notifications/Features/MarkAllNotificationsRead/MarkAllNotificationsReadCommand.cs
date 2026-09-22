using System;

namespace ResolveOps.Modules.Notifications.Features.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(Guid UserId);
