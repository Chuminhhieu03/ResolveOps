using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Messaging;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="OutboxMessage"/> entity (spec §15.11).
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.TenantId);

        builder.Property(m => m.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.EventVersion).IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(m => m.OccurredAtUtc).IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.CausationId)
            .HasMaxLength(100);

        builder.Property(m => m.PartitionKey)
            .HasMaxLength(150);

        builder.Property(m => m.ProcessingStatus)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.ProcessingAttempts).IsRequired();
        builder.Property(m => m.NextAttemptAtUtc);
        builder.Property(m => m.ProcessedAtUtc);

        builder.Property(m => m.LastError)
            .HasColumnType("nvarchar(max)");

        // spec §15.13 — required index for the publisher poll query
        builder.HasIndex(m => new { m.ProcessingStatus, m.NextAttemptAtUtc, m.OccurredAtUtc })
            .HasDatabaseName("IX_OutboxMessages_StatusNextAttempt");
    }
}
