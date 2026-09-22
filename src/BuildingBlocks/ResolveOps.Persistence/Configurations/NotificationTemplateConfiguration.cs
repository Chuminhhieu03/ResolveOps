using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Notifications;

namespace ResolveOps.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Version)
            .IsRequired();

        builder.Property(t => t.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.BodyTemplate)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedAtUtc);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(t => new { t.TenantId, t.TemplateCode, t.Channel, t.Version })
            .IsUnique()
            .HasDatabaseName("uix_notification_templates_tenant_code_channel_ver");
    }
}
