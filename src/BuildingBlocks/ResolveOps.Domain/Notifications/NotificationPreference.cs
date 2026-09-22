using System;

namespace ResolveOps.Domain.Notifications;

public sealed class NotificationPreference : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationClass { get; private set; } = string.Empty;
    public string Channel { get; private set; } = NotificationChannel.InApp;
    public bool IsEnabled { get; private set; } = true;
    public bool IsMandatory { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private NotificationPreference() { } // EF Core

    public static Result<NotificationPreference> Create(
        Guid tenantId,
        Guid userId,
        string notificationClass,
        string channel,
        bool isEnabled,
        TimeProvider timeProvider)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<NotificationPreference>.Failure(new DomainError("INVALID_TENANT", "Tenant ID is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result<NotificationPreference>.Failure(new DomainError("INVALID_USER", "User ID is required."));
        }

        if (!Notifications.NotificationClass.IsValid(notificationClass))
        {
            return Result<NotificationPreference>.Failure(new DomainError("INVALID_NOTIFICATION_CLASS", $"Invalid notification class: '{notificationClass}'."));
        }

        if (!NotificationChannel.IsValid(channel))
        {
            return Result<NotificationPreference>.Failure(new DomainError("INVALID_CHANNEL", $"Invalid notification channel: '{channel}'."));
        }

        var isMandatory = Notifications.NotificationClass.IsMandatory(notificationClass);
        if (isMandatory && !isEnabled)
        {
            return Result<NotificationPreference>.Failure(new DomainError(
                "MANDATORY_NOTIFICATION_CANNOT_BE_DISABLED",
                $"Notification class '{notificationClass}' is mandatory and cannot be disabled."));
        }

        var now = timeProvider.GetUtcNow();

        var preference = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            NotificationClass = notificationClass,
            Channel = channel,
            IsEnabled = isEnabled,
            IsMandatory = isMandatory,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return Result<NotificationPreference>.Success(preference);
    }

    public Result SetEnabled(bool enabled, TimeProvider timeProvider)
    {
        if (IsMandatory && !enabled)
        {
            return Result.Failure(new DomainError(
                "MANDATORY_NOTIFICATION_CANNOT_BE_DISABLED",
                $"Notification class '{NotificationClass}' is mandatory and cannot be disabled."));
        }

        IsEnabled = enabled;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        return Result.Success();
    }
}
