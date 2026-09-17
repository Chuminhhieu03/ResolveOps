using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ExceptionOccurrence"/> (spec §15.7).
/// </summary>
public sealed class ExceptionOccurrenceConfiguration : IEntityTypeConfiguration<ExceptionOccurrence>
{
    public void Configure(EntityTypeBuilder<ExceptionOccurrence> builder)
    {
        builder.ToTable("exception_occurrences");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.TenantId).IsRequired();
        builder.Property(o => o.CaseId).IsRequired();
        builder.Property(o => o.TrackingEventId);

        builder.Property(o => o.OccurrenceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.ObservedAtUtc).IsRequired();

        builder.Property(o => o.Summary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.CaseId })
            .HasDatabaseName("IX_ExceptionOccurrences_TenantCase");
    }
}
