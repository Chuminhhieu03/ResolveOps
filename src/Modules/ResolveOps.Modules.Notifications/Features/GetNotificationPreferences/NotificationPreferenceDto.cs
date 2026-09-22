namespace ResolveOps.Modules.Notifications.Features.GetNotificationPreferences;

public sealed record NotificationPreferenceDto(
    string NotificationClass,
    string Channel,
    bool IsEnabled,
    bool IsMandatory);
