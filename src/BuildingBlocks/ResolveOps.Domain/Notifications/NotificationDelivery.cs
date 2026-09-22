using System;

namespace ResolveOps.Domain.Notifications;

public sealed class NotificationDelivery : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? NotificationId { get; private set; }
    public string Channel { get; private set; } = NotificationChannel.Email;
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Status { get; private set; } = DeliveryStatus.Pending;
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private NotificationDelivery() { } // EF Core

    public static Result<NotificationDelivery> Create(
        Guid tenantId,
        Guid? notificationId,
        string channel,
        string recipient,
        string subject,
        string idempotencyKey,
        TimeProvider timeProvider)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<NotificationDelivery>.Failure(new DomainError("INVALID_TENANT", "Tenant ID is required."));
        }

        if (!NotificationChannel.IsValid(channel))
        {
            return Result<NotificationDelivery>.Failure(new DomainError("INVALID_CHANNEL", $"Invalid delivery channel: '{channel}'."));
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            return Result<NotificationDelivery>.Failure(new DomainError("INVALID_RECIPIENT", "Recipient is required."));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result<NotificationDelivery>.Failure(new DomainError("INVALID_SUBJECT", "Subject is required."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<NotificationDelivery>.Failure(new DomainError("INVALID_IDEMPOTENCY_KEY", "Idempotency key is required."));
        }

        var now = timeProvider.GetUtcNow();

        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NotificationId = notificationId,
            Channel = channel,
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Status = DeliveryStatus.Pending,
            IdempotencyKey = idempotencyKey.Trim(),
            AttemptCount = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return Result<NotificationDelivery>.Success(delivery);
    }

    public void MarkSent(string? providerMessageId, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        Status = DeliveryStatus.Sent;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        SentAtUtc = now;
        ErrorMessage = null;
        AttemptCount++;
        UpdatedAtUtc = now;
    }

    public void RecordFailure(string error, bool canRetry, TimeSpan? retryDelay, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        AttemptCount++;
        ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Unknown delivery failure" : error.Trim();
        if (canRetry)
        {
            Status = DeliveryStatus.Retrying;
            NextAttemptAtUtc = now.Add(retryDelay ?? TimeSpan.FromMinutes(5));
        }
        else
        {
            Status = DeliveryStatus.Failed;
            NextAttemptAtUtc = null;
        }
        UpdatedAtUtc = now;
    }
}
