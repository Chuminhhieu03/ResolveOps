using System;

namespace ResolveOps.Domain.Notifications;

public sealed class Notification : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationClass { get; private set; } = string.Empty;
    public string Channel { get; private set; } = NotificationChannel.InApp;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? DataJson { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private Notification() { } // EF Core

    public static Result<Notification> Create(
        Guid tenantId,
        Guid userId,
        string notificationClass,
        string channel,
        string title,
        string message,
        string? dataJson,
        TimeProvider timeProvider)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<Notification>.Failure(new DomainError("INVALID_TENANT", "Tenant ID is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result<Notification>.Failure(new DomainError("INVALID_USER", "User ID is required."));
        }

        if (!Notifications.NotificationClass.IsValid(notificationClass))
        {
            return Result<Notification>.Failure(new DomainError("INVALID_NOTIFICATION_CLASS", $"Invalid notification class: '{notificationClass}'."));
        }

        if (!NotificationChannel.IsValid(channel))
        {
            return Result<Notification>.Failure(new DomainError("INVALID_CHANNEL", $"Invalid notification channel: '{channel}'."));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<Notification>.Failure(new DomainError("INVALID_TITLE", "Notification title is required."));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Result<Notification>.Failure(new DomainError("INVALID_MESSAGE", "Notification message is required."));
        }

        var now = timeProvider.GetUtcNow();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            NotificationClass = notificationClass,
            Channel = channel,
            Title = title.Trim(),
            Message = message.Trim(),
            DataJson = string.IsNullOrWhiteSpace(dataJson) ? null : dataJson.Trim(),
            IsRead = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return Result<Notification>.Success(notification);
    }

    public void MarkAsRead(TimeProvider timeProvider)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = timeProvider.GetUtcNow();
            UpdatedAtUtc = ReadAtUtc;
        }
    }
}
