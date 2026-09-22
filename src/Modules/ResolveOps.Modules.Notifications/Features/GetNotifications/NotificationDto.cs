using System;

namespace ResolveOps.Modules.Notifications.Features.GetNotifications;

public sealed record NotificationDto(
    Guid Id,
    string NotificationClass,
    string Channel,
    string Title,
    string Message,
    string? DataJson,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc);
