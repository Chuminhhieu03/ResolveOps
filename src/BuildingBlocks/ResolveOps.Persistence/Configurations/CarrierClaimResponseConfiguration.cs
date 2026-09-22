using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Persistence.Configurations;

public class CarrierClaimResponseConfiguration : IEntityTypeConfiguration<CarrierClaimResponse>
{
    public void Configure(EntityTypeBuilder<CarrierClaimResponse> builder)
    {
        builder.ToTable("carrier_claim_responses");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ResponseType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.CarrierReference)
            .HasMaxLength(150);

        builder.Property(r => r.ApprovedAmount)
            .HasPrecision(19, 4);

        builder.Property(r => r.Currency)
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(r => r.ReasonCodes)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

        builder.Property(r => r.SourceChannel)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedAtUtc);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => new { r.TenantId, r.ClaimId })
            .HasDatabaseName("ix_carrier_claim_responses_tenant_claim");
    }
}
