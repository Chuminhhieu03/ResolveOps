using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Reporting;

namespace ResolveOps.Persistence.Configurations;

public class CarrierPerformanceSnapshotConfiguration : IEntityTypeConfiguration<CarrierPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<CarrierPerformanceSnapshot> builder)
    {
        builder.ToTable("carrier_performance_snapshots");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.CarrierId)
            .IsRequired();

        builder.Property(c => c.CarrierName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PeriodDate)
            .IsRequired();

        builder.Property(c => c.TotalShipments)
            .IsRequired();

        builder.Property(c => c.OnTimeShipments)
            .IsRequired();

        builder.Property(c => c.DelayedShipments)
            .IsRequired();

        builder.Property(c => c.ExceptionCount)
            .IsRequired();

        builder.Property(c => c.CriticalSeverityCount)
            .IsRequired();

        builder.Property(c => c.HighSeverityCount)
            .IsRequired();

        builder.Property(c => c.MediumSeverityCount)
            .IsRequired();

        builder.Property(c => c.LowSeverityCount)
            .IsRequired();

        builder.Property(c => c.TotalClaims)
            .IsRequired();

        builder.Property(c => c.ApprovedClaims)
            .IsRequired();

        builder.Property(c => c.RejectedClaims)
            .IsRequired();

        builder.Property(c => c.TotalClaimedAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(c => c.TotalApprovedAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(c => c.TotalRecoveredAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(c => c.AvgResponseTimeHours)
            .IsRequired();

        builder.Property(c => c.LastCalculatedAtUtc)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedAtUtc);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        builder.HasIndex(c => new { c.TenantId, c.CarrierId, c.PeriodDate })
            .IsUnique()
            .HasDatabaseName("ix_carrier_performance_snapshots_tenant_carrier_period");

        builder.HasIndex(c => new { c.TenantId, c.PeriodDate })
            .HasDatabaseName("ix_carrier_performance_snapshots_tenant_period");
    }
}
