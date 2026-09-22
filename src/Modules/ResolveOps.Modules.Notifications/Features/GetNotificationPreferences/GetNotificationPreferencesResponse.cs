using System.Collections.Generic;

namespace ResolveOps.Modules.Notifications.Features.GetNotificationPreferences;

public sealed record GetNotificationPreferencesResponse(
    IReadOnlyList<NotificationPreferenceDto> Preferences);
