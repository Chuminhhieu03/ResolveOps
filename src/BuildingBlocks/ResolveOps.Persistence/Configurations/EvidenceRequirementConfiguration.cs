using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Documents;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="EvidenceRequirement"/> (spec §15.9).
/// </summary>
public sealed class EvidenceRequirementConfiguration : IEntityTypeConfiguration<EvidenceRequirement>
{
    public void Configure(EntityTypeBuilder<EvidenceRequirement> builder)
    {
        builder.ToTable("evidence_requirements");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.PolicyVersionId).IsRequired();

        builder.Property(r => r.ExceptionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ClaimType)
            .HasMaxLength(50);

        builder.Property(r => r.EvidenceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.IsMandatory).IsRequired();

        builder.Property(r => r.ConditionJson);

        // Filter by policy version
        builder.HasIndex(r => new { r.TenantId, r.PolicyVersionId })
            .HasDatabaseName("ix_evidence_requirements_tenant_policy_version");

        // Checklist query: exception type + mandatory flag
        builder.HasIndex(r => new { r.TenantId, r.ExceptionType, r.EvidenceType })
            .HasDatabaseName("ix_evidence_requirements_tenant_exception_evidence");
    }
}
