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

        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.Property(s => s.Version)
            .IsConcurrencyToken()
            .IsRequired();
    }
}
