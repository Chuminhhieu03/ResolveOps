using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Messaging;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="IdempotencyRecord"/> entity (spec §15.11).
/// Composite primary key: (TenantId, Scope, IdempotencyKey).
/// </summary>
public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        // Composite PK per spec §15.11
        builder.HasKey(r => new { r.TenantId, r.Scope, r.IdempotencyKey });

        builder.Property(r => r.TenantId).IsRequired();

        builder.Property(r => r.Scope)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.RequestHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.ResponseStatus).IsRequired();

        builder.Property(r => r.ResponseBody)
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.ResourceId);

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.ExpiresAtUtc).IsRequired();
    }
}
