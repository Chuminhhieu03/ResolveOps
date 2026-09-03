using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Tenancy;

namespace ResolveOps.Modules.Tenancy.Infrastructure.Persistence;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.DefaultTimezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.DefaultCurrency)
            .HasColumnType("nchar(3)")
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.UpdatedAtUtc).IsRequired();

        builder.Property(t => t.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // spec §15.2 — code is globally unique (not tenant-scoped: Tenant IS the isolation boundary)
        builder.HasIndex(t => t.Code).IsUnique();
    }
}
