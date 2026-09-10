using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Tenancy;

namespace ResolveOps.Modules.Tenancy.Infrastructure.Persistence;

public sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("tenant_settings");

        // PK is also FK to tenants (one-to-one, spec §15.2)
        builder.HasKey(s => s.TenantId);

        builder.Property(s => s.TenantId)
            .ValueGeneratedNever();

        builder.Property(s => s.SettingsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(255);
        builder.Property(s => s.UpdatedAtUtc);
        builder.Property(s => s.UpdatedBy).HasMaxLength(255);

        builder.Property(s => s.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(36)
            .IsRequired();
    }
}
