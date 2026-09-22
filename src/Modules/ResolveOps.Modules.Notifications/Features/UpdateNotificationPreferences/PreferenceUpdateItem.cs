namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public sealed record PreferenceUpdateItem(
    string NotificationClass,
    string Channel,
    bool IsEnabled);
