using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Notifications;

namespace ResolveOps.Persistence.Configurations;

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId)
            .IsRequired();

        builder.Property(d => d.NotificationId);

        builder.Property(d => d.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Recipient)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(d => d.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(d => d.AttemptCount)
            .IsRequired();

        builder.Property(d => d.NextAttemptAtUtc);
        builder.Property(d => d.SentAtUtc);

        builder.Property(d => d.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedAtUtc);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);

        // Unique index on (tenant_id, idempotency_key) to strictly prevent duplicate dispatches
        builder.HasIndex(d => new { d.TenantId, d.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uix_notification_deliveries_tenant_idempotency");

        builder.HasIndex(d => new { d.TenantId, d.Status, d.NextAttemptAtUtc })
            .HasDatabaseName("ix_notification_deliveries_tenant_status_next");
    }
}
