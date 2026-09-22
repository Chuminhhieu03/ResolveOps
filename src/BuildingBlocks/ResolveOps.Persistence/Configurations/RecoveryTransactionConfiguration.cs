using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Persistence.Configurations;

public class RecoveryTransactionConfiguration : IEntityTypeConfiguration<RecoveryTransaction>
{
    public void Configure(EntityTypeBuilder<RecoveryTransaction> builder)
    {
        builder.ToTable("recovery_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.ExternalReference)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Amount)
            .HasPrecision(19, 4);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(t => t.ReceivedAtUtc)
            .IsRequired();

        builder.Property(t => t.RecordedBy)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedAtUtc);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);

        // Unique index: (tenant_id, external_reference) (spec §15.10, Invariant 11, Edge Case 17)
        builder.HasIndex(t => new { t.TenantId, t.ExternalReference })
            .IsUnique()
            .HasDatabaseName("uix_recovery_transactions_tenant_ref");

        // Index on (tenant_id, claim_id) for efficient claim recovery queries
        builder.HasIndex(t => new { t.TenantId, t.ClaimId })
            .HasDatabaseName("ix_recovery_transactions_tenant_claim");
    }
}
