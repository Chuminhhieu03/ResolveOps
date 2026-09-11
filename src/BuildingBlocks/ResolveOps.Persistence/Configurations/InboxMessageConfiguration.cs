using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Messaging;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="InboxMessage"/> entity (spec §15.11).
/// Composite primary key: (ConsumerName, MessageId).
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        // Composite PK per spec §15.11
        builder.HasKey(m => new { m.ConsumerName, m.MessageId });

        builder.Property(m => m.ConsumerName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.MessageId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.TenantId);

        builder.Property(m => m.ReceivedAtUtc).IsRequired();
        builder.Property(m => m.ProcessedAtUtc);

        builder.Property(m => m.ResultHash)
            .HasMaxLength(128);
    }
}
