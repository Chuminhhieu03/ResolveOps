using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Tracking;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="InboundEventReceipt"/> (spec §15.6).
/// </summary>
public sealed class InboundEventReceiptConfiguration : IEntityTypeConfiguration<InboundEventReceipt>
{
    public void Configure(EntityTypeBuilder<InboundEventReceipt> builder)
    {
        builder.ToTable("inbound_event_receipts");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.CarrierId);

        builder.Property(r => r.SourceSystem)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ExternalEventId)
            .HasMaxLength(150);

        builder.Property(r => r.IdempotencyKey)
            .HasMaxLength(200);

        builder.Property(r => r.PayloadHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.RawPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(r => r.RawPayloadUri)
            .HasMaxLength(1000);

        builder.Property(r => r.ReceivedAtUtc).IsRequired();
        builder.Property(r => r.SignatureValid);

        builder.Property(r => r.ProcessingStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.FailureCode)
            .HasMaxLength(100);

        builder.Property(r => r.FailureDetail)
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        // Unique index for webhook duplicate/replay guard (spec §15.6, §24 Phase 6 task 3)
        builder.HasIndex(r => new { r.TenantId, r.SourceSystem, r.ExternalEventId })
            .IsUnique()
            .HasFilter("[external_event_id] IS NOT NULL")
            .HasDatabaseName("UIX_InboundEventReceipts_TenantSourceExternalEvent");
    }
}
