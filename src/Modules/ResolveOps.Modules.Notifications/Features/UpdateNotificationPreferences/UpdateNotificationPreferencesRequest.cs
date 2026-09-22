using System.Collections.Generic;

namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesRequest(
    IReadOnlyList<PreferenceUpdateItem> Preferences);
