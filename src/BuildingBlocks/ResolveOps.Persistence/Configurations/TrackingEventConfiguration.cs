using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Tracking;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TrackingEvent"/> (spec §15.6).
/// </summary>
public sealed class TrackingEventConfiguration : IEntityTypeConfiguration<TrackingEvent>
{
    public void Configure(EntityTypeBuilder<TrackingEvent> builder)
    {
        builder.ToTable("tracking_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ShipmentId).IsRequired();
        builder.Property(e => e.ShipmentLegId);
        builder.Property(e => e.CarrierId).IsRequired();
        builder.Property(e => e.InboundReceiptId).IsRequired();

        builder.Property(e => e.ExternalEventId)
            .HasMaxLength(150);

        builder.Property(e => e.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventCode)
            .HasMaxLength(100);

        builder.Property(e => e.OccurredAtUtc).IsRequired();
        builder.Property(e => e.ReceivedAtUtc).IsRequired();

        builder.Property(e => e.LocationId);
        builder.Property(e => e.LocationText)
            .HasMaxLength(500);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.PackageCount);
        builder.Property(e => e.CorrectionOfEventId);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CausationId)
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAtUtc).IsRequired();

        // Important indexes (spec §15.13)
        builder.HasIndex(e => new { e.TenantId, e.ShipmentId, e.OccurredAtUtc, e.Id })
            .HasDatabaseName("IX_TrackingEvents_ShipmentTimeline");

        builder.HasIndex(e => new { e.TenantId, e.CarrierId, e.ExternalEventId })
            .HasDatabaseName("IX_TrackingEvents_CarrierExternalEvent");

        // High-performance index for tenant-scoped reporting and dashboard queries
        builder.HasIndex(e => new { e.TenantId, e.CreatedAtUtc })
            .HasDatabaseName("IX_TrackingEvents_TenantCreatedAt");
    }
}
