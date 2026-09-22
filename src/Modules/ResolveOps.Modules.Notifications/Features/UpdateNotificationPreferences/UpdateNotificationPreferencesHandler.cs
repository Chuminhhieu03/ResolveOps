using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Notifications;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public UpdateNotificationPreferencesHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UpdateNotificationPreferencesResponse>> HandleAsync(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var existingPreferences = await _dbContext.NotificationPreferences
            .Where(p => p.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        int updatedCount = 0;

        foreach (var item in command.Preferences)
        {
            var isMandatory = NotificationClass.IsMandatory(item.NotificationClass);

            // If user attempts to disable a mandatory notification class, reject according to spec §12 / §16
            if (isMandatory && !item.IsEnabled)
            {
                return Result<UpdateNotificationPreferencesResponse>.Failure(new DomainError(
                    "CANNOT_DISABLE_MANDATORY_NOTIFICATION",
                    $"Notification class '{item.NotificationClass}' is mandatory and cannot be disabled."));
            }

            var existing = existingPreferences.FirstOrDefault(p =>
                p.NotificationClass.Equals(item.NotificationClass, StringComparison.OrdinalIgnoreCase) &&
                p.Channel.Equals(item.Channel, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var setResult = existing.SetEnabled(item.IsEnabled, _timeProvider);
                if (setResult.IsFailure)
                {
                    return Result<UpdateNotificationPreferencesResponse>.Failure(setResult.Error);
                }
                updatedCount++;
            }
            else
            {
                var createResult = NotificationPreference.Create(
                    tenantId,
                    command.UserId,
                    item.NotificationClass,
                    item.Channel,
                    item.IsEnabled,
                    _timeProvider);

                if (createResult.IsFailure)
                {
                    return Result<UpdateNotificationPreferencesResponse>.Failure(createResult.Error);
                }

                _dbContext.NotificationPreferences.Add(createResult.Value);
                updatedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateNotificationPreferencesResponse>.Success(
            new UpdateNotificationPreferencesResponse(true, updatedCount));
    }
}
