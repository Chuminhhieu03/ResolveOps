using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Notifications;

namespace ResolveOps.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.TenantId)
            .IsRequired();

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.NotificationClass)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.DataJson);

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.Property(n => n.ReadAtUtc);

        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.CreatedBy).HasMaxLength(100);
        builder.Property(n => n.UpdatedAtUtc);
        builder.Property(n => n.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead, n.CreatedAtUtc })
            .HasDatabaseName("ix_notifications_tenant_user_read_created");
    }
}
