using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Tracking;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="QuarantinedEvent"/> (spec §15.6).
/// </summary>
public sealed class QuarantinedEventConfiguration : IEntityTypeConfiguration<QuarantinedEvent>
{
    public void Configure(EntityTypeBuilder<QuarantinedEvent> builder)
    {
        builder.ToTable("quarantined_events");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();

        builder.Property(q => q.TenantId).IsRequired();
        builder.Property(q => q.InboundReceiptId).IsRequired();

        builder.Property(q => q.ReasonCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(q => q.Detail)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(q => q.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(q => q.AssignedUserId);
        builder.Property(q => q.ResolvedShipmentId);
        builder.Property(q => q.ResolvedAtUtc);
        builder.Property(q => q.CreatedAtUtc).IsRequired();

        // Optimistic concurrency (ADR-006)
        builder.Property(q => q.ConcurrencyStamp)
            .HasMaxLength(50)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(q => new { q.TenantId, q.Status, q.CreatedAtUtc })
            .HasDatabaseName("IX_QuarantinedEvents_TenantStatusCreated");
    }
}
