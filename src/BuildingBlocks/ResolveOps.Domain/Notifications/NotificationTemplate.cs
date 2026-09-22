using System;
using System.Collections.Generic;

namespace ResolveOps.Domain.Notifications;

public sealed class NotificationTemplate : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public string TemplateCode { get; private set; } = string.Empty;
    public string Channel { get; private set; } = NotificationChannel.InApp;
    public int Version { get; private set; } = 1;
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private NotificationTemplate() { } // EF Core

    public static Result<NotificationTemplate> Create(
        Guid? tenantId,
        string templateCode,
        string channel,
        int version,
        string subjectTemplate,
        string bodyTemplate,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
        {
            return Result<NotificationTemplate>.Failure(new DomainError("INVALID_TEMPLATE_CODE", "Template code is required."));
        }

        if (!NotificationChannel.IsValid(channel))
        {
            return Result<NotificationTemplate>.Failure(new DomainError("INVALID_CHANNEL", $"Invalid notification channel: '{channel}'."));
        }

        if (version <= 0)
        {
            return Result<NotificationTemplate>.Failure(new DomainError("INVALID_VERSION", "Template version must be positive."));
        }

        if (string.IsNullOrWhiteSpace(subjectTemplate))
        {
            return Result<NotificationTemplate>.Failure(new DomainError("INVALID_SUBJECT", "Subject template is required."));
        }

        if (string.IsNullOrWhiteSpace(bodyTemplate))
        {
            return Result<NotificationTemplate>.Failure(new DomainError("INVALID_BODY", "Body template is required."));
        }

        var now = timeProvider.GetUtcNow();

        var template = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateCode = templateCode.Trim(),
            Channel = channel,
            Version = version,
            SubjectTemplate = subjectTemplate.Trim(),
            BodyTemplate = bodyTemplate.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return Result<NotificationTemplate>.Success(template);
    }

    public (string Subject, string Body) Render(IReadOnlyDictionary<string, string> placeholders)
    {
        var subject = SubjectTemplate;
        var body = BodyTemplate;

        if (placeholders != null)
        {
            foreach (var kvp in placeholders)
            {
                var token = "{{" + kvp.Key + "}}";
                subject = subject.Replace(token, kvp.Value, StringComparison.OrdinalIgnoreCase);
                body = body.Replace(token, kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return (subject, body);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
