using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Notifications;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Notifications.Features.GetNotificationPreferences;

public class GetNotificationPreferencesHandler
{
    private readonly AppDbContext _dbContext;

    public GetNotificationPreferencesHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetNotificationPreferencesResponse>> HandleAsync(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        var existingPreferences = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == query.UserId)
            .ToListAsync(cancellationToken);

        // If user has not explicitly configured any preferences yet, build defaults
        var allClasses = NotificationClass.All;
        var channels = new[] { NotificationChannel.InApp, NotificationChannel.Email };

        var dtoList = new List<NotificationPreferenceDto>();

        foreach (var cls in allClasses)
        {
            var isMandatory = NotificationClass.IsMandatory(cls);
            foreach (var ch in channels)
            {
                var existing = existingPreferences.FirstOrDefault(p =>
                    p.NotificationClass.Equals(cls, StringComparison.OrdinalIgnoreCase) &&
                    p.Channel.Equals(ch, StringComparison.OrdinalIgnoreCase));

                var isEnabled = existing != null ? existing.IsEnabled : true;
                dtoList.Add(new NotificationPreferenceDto(cls, ch, isEnabled, isMandatory));
            }
        }

        return Result<GetNotificationPreferencesResponse>.Success(
            new GetNotificationPreferencesResponse(dtoList));
    }
}
