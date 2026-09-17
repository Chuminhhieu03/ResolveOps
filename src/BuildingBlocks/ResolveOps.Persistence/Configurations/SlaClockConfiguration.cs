using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SlaClock"/> (spec §15.8).
/// </summary>
public sealed class SlaClockConfiguration : IEntityTypeConfiguration<SlaClock>
{
    public void Configure(EntityTypeBuilder<SlaClock> builder)
    {
        builder.ToTable("sla_clocks");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.CaseId).IsRequired();
        builder.Property(c => c.ClaimId);

        builder.Property(c => c.ClockType)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(c => c.PolicyVersionId).IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.StartedAtUtc);
        builder.Property(c => c.DueAtUtc);
        builder.Property(c => c.PausedAtUtc);
        builder.Property(c => c.TotalPausedSeconds).IsRequired();
        builder.Property(c => c.CompletedAtUtc);
        builder.Property(c => c.BreachedAtUtc);

        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedAtUtc);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.Property(c => c.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        // Indexes for SLA breach monitoring and case lookups (spec §15.8, §18.1)
        builder.HasIndex(c => new { c.TenantId, c.Status, c.DueAtUtc })
            .HasDatabaseName("ix_sla_clocks_tenant_status_due");

        builder.HasIndex(c => new { c.TenantId, c.CaseId, c.ClockType })
            .HasDatabaseName("ix_sla_clocks_tenant_case_clock");

        // Cascade delete pauses with clock
        builder.HasMany(c => c.Pauses)
            .WithOne()
            .HasForeignKey(p => p.SlaClockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
