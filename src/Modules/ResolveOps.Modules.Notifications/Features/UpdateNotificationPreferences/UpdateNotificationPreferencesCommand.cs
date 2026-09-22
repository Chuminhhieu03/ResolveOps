using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid UserId,
    IReadOnlyList<PreferenceUpdateItem> Preferences);
