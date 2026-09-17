using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SlaPolicyVersion"/> (spec §15.8).
/// </summary>
public sealed class SlaPolicyVersionConfiguration : IEntityTypeConfiguration<SlaPolicyVersion>
{
    public void Configure(EntityTypeBuilder<SlaPolicyVersion> builder)
    {
        builder.ToTable("sla_policy_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.SlaPolicyId).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.CalendarId);

        builder.Property(v => v.AcknowledgementMinutes);
        builder.Property(v => v.FirstActionMinutes);
        builder.Property(v => v.ResolutionMinutes);
        builder.Property(v => v.ClaimSubmissionMinutes);

        builder.Property(v => v.PauseReasonCodes);
        builder.Property(v => v.EffectiveFromUtc).IsRequired();
        builder.Property(v => v.EffectiveToUtc);

        builder.Property(v => v.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc).IsRequired();
        builder.Property(v => v.CreatedBy).HasMaxLength(100);
        builder.Property(v => v.UpdatedAtUtc);
        builder.Property(v => v.UpdatedBy).HasMaxLength(100);

        builder.Property(v => v.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(v => new { v.TenantId, v.SlaPolicyId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uix_sla_policy_versions_tenant_policy_ver");
    }
}
