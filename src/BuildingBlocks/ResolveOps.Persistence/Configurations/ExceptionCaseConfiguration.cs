using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ExceptionCase"/> (spec §15.7).
/// </summary>
public sealed class ExceptionCaseConfiguration : IEntityTypeConfiguration<ExceptionCase>
{
    public void Configure(EntityTypeBuilder<ExceptionCase> builder)
    {
        builder.ToTable("exception_cases");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.CaseNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(c => c.ShipmentId).IsRequired();
        builder.Property(c => c.ShipmentLegId);

        builder.Property(c => c.ExceptionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Fingerprint)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(c => c.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.SeverityScore);

        builder.Property(c => c.PolicyId).IsRequired();
        builder.Property(c => c.PolicyVersionNumber).IsRequired();

        builder.Property(c => c.OwnerUserId);
        builder.Property(c => c.OwnerTeamCode).HasMaxLength(50);

        builder.Property(c => c.FinancialExposure)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(c => c.ExposureCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(c => c.RootCauseCode).HasMaxLength(100);
        builder.Property(c => c.DispositionCode).HasMaxLength(100);

        builder.Property(c => c.DetectedAtUtc).IsRequired();
        builder.Property(c => c.ResolvedAtUtc);
        builder.Property(c => c.ClosedAtUtc);

        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedAtUtc);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        // Optimistic concurrency token (ADR-006)
        builder.Property(c => c.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        // Unique case number per tenant
        builder.HasIndex(c => new { c.TenantId, c.CaseNumber })
            .IsUnique()
            .HasDatabaseName("UIX_ExceptionCases_TenantCaseNumber");

        // Filtered unique partial index for active fingerprints (spec §15.7, §24 Phase 7)
        builder.HasIndex(c => new { c.TenantId, c.Fingerprint })
            .IsUnique()
            .HasFilter("[status] NOT IN ('Closed', 'Cancelled')")
            .HasDatabaseName("UIX_ExceptionCases_ActiveFingerprint");

        // Search indexes
        builder.HasIndex(c => new { c.TenantId, c.ShipmentId })
            .HasDatabaseName("IX_ExceptionCases_TenantShipment");

        builder.HasIndex(c => new { c.TenantId, c.Status, c.Severity })
            .HasDatabaseName("IX_ExceptionCases_TenantStatusSeverity");

        // High-performance indexes for case list filtering, pagination, and dashboard metrics
        builder.HasIndex(c => new { c.TenantId, c.DetectedAtUtc })
            .HasDatabaseName("IX_ExceptionCases_TenantDetectedAt");

        builder.HasIndex(c => new { c.TenantId, c.CreatedAtUtc })
            .HasDatabaseName("IX_ExceptionCases_TenantCreatedAt");

        builder.HasIndex(c => new { c.TenantId, c.Status, c.DetectedAtUtc })
            .HasDatabaseName("IX_ExceptionCases_TenantStatusDetectedAt");

        // Relationships
        builder.HasMany(c => c.Occurrences)
            .WithOne()
            .HasForeignKey(o => o.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.TimelineEntries)
            .WithOne()
            .HasForeignKey(t => t.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
