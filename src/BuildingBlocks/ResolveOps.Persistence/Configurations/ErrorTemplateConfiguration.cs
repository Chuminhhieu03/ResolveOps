using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain;

namespace ResolveOps.Persistence.Configurations;

public sealed class ErrorTemplateConfiguration : IEntityTypeConfiguration<ErrorTemplate>
{
    public void Configure(EntityTypeBuilder<ErrorTemplate> builder)
    {
        builder.ToTable("error_templates");

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MessageTemplate)
            .HasColumnName("message_template")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.HttpStatusCode)
            .HasColumnName("http_status_code")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(255);

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(255);
    }
}
