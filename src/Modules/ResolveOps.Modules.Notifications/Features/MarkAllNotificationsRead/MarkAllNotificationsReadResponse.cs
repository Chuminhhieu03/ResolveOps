namespace ResolveOps.Modules.Notifications.Features.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadResponse(
    bool Success,
    int UpdatedCount);
