using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SlaClockPause"/> (spec §15.8).
/// </summary>
public sealed class SlaClockPauseConfiguration : IEntityTypeConfiguration<SlaClockPause>
{
    public void Configure(EntityTypeBuilder<SlaClockPause> builder)
    {
        builder.ToTable("sla_clock_pauses");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.SlaClockId).IsRequired();

        builder.Property(p => p.ReasonCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.StartedAtUtc).IsRequired();
        builder.Property(p => p.EndedAtUtc);
        builder.Property(p => p.StartedBy);
        builder.Property(p => p.EndedBy);

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.TenantId, p.SlaClockId })
            .HasDatabaseName("ix_sla_clock_pauses_tenant_clock");
    }
}
