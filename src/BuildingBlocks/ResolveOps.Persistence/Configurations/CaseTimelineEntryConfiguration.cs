using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="CaseTimelineEntry"/> (spec §15.7).
/// </summary>
public sealed class CaseTimelineEntryConfiguration : IEntityTypeConfiguration<CaseTimelineEntry>
{
    public void Configure(EntityTypeBuilder<CaseTimelineEntry> builder)
    {
        builder.ToTable("case_timeline_entries");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.CaseId).IsRequired();

        builder.Property(t => t.EntryType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ActorType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ActorId);

        builder.Property(t => t.Summary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.DetailsJson);

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Property(t => t.CorrelationId)
            .HasMaxLength(100);

        builder.HasIndex(t => new { t.TenantId, t.CaseId, t.CreatedAtUtc })
            .HasDatabaseName("IX_CaseTimelineEntries_TenantCaseCreatedAt");
    }
}
