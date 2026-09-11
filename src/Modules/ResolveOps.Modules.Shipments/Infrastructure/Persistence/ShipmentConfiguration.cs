using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Shipments;

namespace ResolveOps.Modules.Shipments.Infrastructure.Persistence;

/// <summary>
/// EF Core configurations for all shipment tables (spec §15.5).
/// Registered via AppDbContext.AddConfigurationAssembly() in ShipmentsModule.
/// </summary>
public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.TenantId).IsRequired();

        builder.Property(s => s.ExternalReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.SourceSystem)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CustomerId).IsRequired();
        builder.Property(s => s.OriginLocationId).IsRequired();
        builder.Property(s => s.DestinationLocationId).IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.ServiceLevel)
            .HasMaxLength(50);

        builder.Property(s => s.PlannedPickupAtUtc).IsRequired();
        builder.Property(s => s.PlannedDeliveryAtUtc).IsRequired();
        builder.Property(s => s.ActualPickupAtUtc);
        builder.Property(s => s.ActualDeliveryAtUtc);

        builder.Property(s => s.DeclaredValue)
            .HasColumnType("decimal(19,4)");

        builder.Property(s => s.DeclaredValueCurrency)
            .HasColumnType("nchar(3)");

        builder.Property(s => s.ExpectedPackageCount);

        builder.Property(s => s.ExpectedWeight)
            .HasColumnType("decimal(18,3)");

        builder.Property(s => s.WeightUnit)
            .HasMaxLength(10);

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(255);
        builder.Property(s => s.UpdatedAtUtc);
        builder.Property(s => s.UpdatedBy).HasMaxLength(255);

        builder.Property(s => s.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(36)
            .IsRequired();

        // spec §15.5 — unique external reference within tenant+source system
        builder.HasIndex(s => new { s.TenantId, s.SourceSystem, s.ExternalReference })
            .IsUnique()
            .HasDatabaseName("UIX_Shipments_TenantSourceReference");

        // Common operational query: active shipments per tenant
        builder.HasIndex(s => new { s.TenantId, s.Status, s.PlannedDeliveryAtUtc })
            .HasDatabaseName("IX_Shipments_TenantStatusDelivery");

        // Navigation
        builder.HasMany(s => s.Legs)
            .WithOne()
            .HasForeignKey(l => l.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.TrackingAliases)
            .WithOne()
            .HasForeignKey(a => a.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ShipmentLegConfiguration : IEntityTypeConfiguration<ShipmentLeg>
{
    public void Configure(EntityTypeBuilder<ShipmentLeg> builder)
    {
        builder.ToTable("shipment_legs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.TenantId).IsRequired();
        builder.Property(l => l.ShipmentId).IsRequired();
        builder.Property(l => l.SequenceNumber).IsRequired();
        builder.Property(l => l.CarrierId).IsRequired();

        builder.Property(l => l.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(l => l.OriginLocationId).IsRequired();
        builder.Property(l => l.DestinationLocationId).IsRequired();

        builder.Property(l => l.PlannedDepartureAtUtc);
        builder.Property(l => l.PlannedArrivalAtUtc);
        builder.Property(l => l.ActualDepartureAtUtc);
        builder.Property(l => l.ActualArrivalAtUtc);

        builder.Property(l => l.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(l => l.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(36)
            .IsRequired();

        // spec §15.5 — sequence unique within shipment
        builder.HasIndex(l => new { l.TenantId, l.ShipmentId, l.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("UIX_ShipmentLegs_TenantShipmentSeq");
    }
}

public sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("shipment_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.ShipmentId).IsRequired();

        builder.Property(i => i.LineReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Sku)
            .HasMaxLength(100);

        builder.Property(i => i.Description)
            .HasMaxLength(500);

        builder.Property(i => i.ExpectedQuantity)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        builder.Property(i => i.QuantityUnit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.UnitValue)
            .HasColumnType("decimal(19,4)");

        builder.Property(i => i.Currency)
            .HasColumnType("nchar(3)");

        // spec §15.5 — line reference unique within shipment
        builder.HasIndex(i => new { i.TenantId, i.ShipmentId, i.LineReference })
            .IsUnique()
            .HasDatabaseName("UIX_ShipmentItems_TenantShipmentLine");
    }
}

public sealed class ShipmentTrackingAliasConfiguration : IEntityTypeConfiguration<ShipmentTrackingAlias>
{
    public void Configure(EntityTypeBuilder<ShipmentTrackingAlias> builder)
    {
        builder.ToTable("shipment_tracking_aliases");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.ShipmentId).IsRequired();
        builder.Property(a => a.ShipmentLegId);
        builder.Property(a => a.CarrierId).IsRequired();

        builder.Property(a => a.AliasType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.AliasValue)
            .HasMaxLength(150)
            .IsRequired();

        // spec §15.5 — alias unique per tenant+carrier+type+value
        builder.HasIndex(a => new { a.TenantId, a.CarrierId, a.AliasType, a.AliasValue })
            .IsUnique()
            .HasDatabaseName("UIX_ShipmentTrackingAliases_TenantCarrierTypeValue");
    }
}
