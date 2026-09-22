using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Persistence.Configurations;

public class ClaimApprovalConfiguration : IEntityTypeConfiguration<ClaimApproval>
{
    public void Configure(EntityTypeBuilder<ClaimApproval> builder)
    {
        builder.ToTable("claim_approvals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApprovalType)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.ClaimVersion)
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAtUtc).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedAtUtc);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.Property(a => a.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.ClaimId })
            .HasDatabaseName("ix_claim_approvals_tenant_claim");
    }
}
