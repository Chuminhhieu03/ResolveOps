using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SlaPolicy"/> (spec §15.8).
/// </summary>
public sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("sla_policies");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.PolicyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.IsActive).IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.Property(p => p.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.PolicyKey })
            .IsUnique()
            .HasDatabaseName("uix_sla_policies_tenant_key");
    }
}
