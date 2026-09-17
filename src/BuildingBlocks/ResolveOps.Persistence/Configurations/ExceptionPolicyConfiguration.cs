using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ExceptionPolicy"/> (spec §15.7).
/// </summary>
public sealed class ExceptionPolicyConfiguration : IEntityTypeConfiguration<ExceptionPolicy>
{
    public void Configure(EntityTypeBuilder<ExceptionPolicy> builder)
    {
        builder.ToTable("exception_policies");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.PolicyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.VersionNumber).IsRequired();

        builder.Property(p => p.ExceptionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.EffectiveFromUtc).IsRequired();
        builder.Property(p => p.EffectiveToUtc);

        builder.Property(p => p.RuleDefinitionJson).IsRequired();
        builder.Property(p => p.SeverityDefinitionJson).IsRequired();
        builder.Property(p => p.AssignmentDefinitionJson).IsRequired();

        builder.Property(p => p.EvidencePolicyVersionId);
        builder.Property(p => p.SlaPolicyVersionId);

        builder.Property(p => p.CreatedByUserId).IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        // Unique constraint: (tenant_id, policy_key, version_number)
        builder.HasIndex(p => new { p.TenantId, p.PolicyKey, p.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UIX_ExceptionPolicies_TenantKeyVersion");

        builder.HasIndex(p => new { p.TenantId, p.ExceptionType, p.Status })
            .HasDatabaseName("IX_ExceptionPolicies_TenantTypeStatus");
    }
}
