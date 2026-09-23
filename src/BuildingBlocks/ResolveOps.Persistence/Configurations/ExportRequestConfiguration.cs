using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Reporting;

namespace ResolveOps.Persistence.Configurations;

public class ExportRequestConfiguration : IEntityTypeConfiguration<ExportRequest>
{
    public void Configure(EntityTypeBuilder<ExportRequest> builder)
    {
        builder.ToTable("export_requests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.ExportType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.FilterCriteriaJson);

        builder.Property(e => e.Container)
            .HasMaxLength(100);

        builder.Property(e => e.BlobPath)
            .HasMaxLength(500);

        builder.Property(e => e.RowCount);

        builder.Property(e => e.FileSizeBytes);

        builder.Property(e => e.ErrorMessage);

        builder.Property(e => e.CompletedAtUtc);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedAtUtc);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.TenantId, e.UserId, e.Status, e.CreatedAtUtc })
            .HasDatabaseName("ix_export_requests_tenant_user_status_created");

        builder.HasIndex(e => new { e.Status, e.CreatedAtUtc })
            .HasDatabaseName("ix_export_requests_status_created");
    }
}
