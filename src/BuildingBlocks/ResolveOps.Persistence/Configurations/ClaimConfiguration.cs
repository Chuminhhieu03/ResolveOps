using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ClaimType)
            .HasMaxLength(30);

        builder.Property(c => c.Status)
            .HasMaxLength(30);

        builder.Property(c => c.EligibilityStatus)
            .HasMaxLength(30);

        builder.Property(c => c.EligibilityReasonCodes)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

        builder.Property(c => c.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(c => c.ClaimedAmount).HasPrecision(19, 4);
        builder.Property(c => c.ApprovedAmount).HasPrecision(19, 4);
        builder.Property(c => c.RecoveredAmount).HasPrecision(19, 4);
        builder.Property(c => c.WrittenOffAmount).HasPrecision(19, 4);

        builder.Property(c => c.ExternalSubmissionReference)
            .HasMaxLength(100);

        builder.Property(c => c.WriteOffReason).HasMaxLength(200);
        builder.Property(c => c.ClosingNotes).HasMaxLength(1000);

        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);
        builder.Property(c => c.ConcurrencyStamp).IsRequired().HasMaxLength(50);

        builder.HasMany(c => c.LossComponents)
            .WithOne()
            .HasForeignKey(lc => lc.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Approvals)
            .WithOne()
            .HasForeignKey(a => a.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Responses)
            .WithOne()
            .HasForeignKey(r => r.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.RecoveryTransactions)
            .WithOne()
            .HasForeignKey(t => t.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: (tenant_id, claim_number)
        builder.HasIndex(c => new { c.TenantId, c.ClaimNumber })
            .IsUnique()
            .HasDatabaseName("uix_claims_claim_number");

        // Partial unique index: (tenant_id, case_id, carrier_id) WHERE status NOT IN ('Cancelled', 'Closed')
        builder.HasIndex(c => new { c.TenantId, c.CaseId, c.CarrierId })
            .IsUnique()
            .HasFilter("status NOT IN ('Cancelled', 'Closed')")
            .HasDatabaseName("uix_claims_active_case_carrier");

        // Unique index on external submission reference per carrier (spec §10.5 Invariant 10)
        builder.HasIndex(c => new { c.TenantId, c.CarrierId, c.ExternalSubmissionReference })
            .IsUnique()
            .HasFilter("external_submission_reference IS NOT NULL")
            .HasDatabaseName("uix_claims_carrier_submission_ref");

        // High-performance search and case relationship indexes
        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasDatabaseName("ix_claims_tenant_status");

        builder.HasIndex(c => new { c.TenantId, c.CaseId })
            .HasDatabaseName("ix_claims_tenant_case");
    }
}
