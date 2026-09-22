using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Notifications;

namespace ResolveOps.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.NotificationClass)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.IsEnabled)
            .IsRequired();

        builder.Property(p => p.IsMandatory)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.TenantId, p.UserId, p.NotificationClass, p.Channel })
            .IsUnique()
            .HasDatabaseName("uix_notification_preferences_tenant_user_class_channel");
    }
}
