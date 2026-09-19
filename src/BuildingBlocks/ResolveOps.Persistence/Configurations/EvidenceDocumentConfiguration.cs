using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Documents;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="EvidenceDocument"/> (spec §15.9).
/// </summary>
public sealed class EvidenceDocumentConfiguration : IEntityTypeConfiguration<EvidenceDocument>
{
    public void Configure(EntityTypeBuilder<EvidenceDocument> builder)
    {
        builder.ToTable("evidence_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CaseId).IsRequired();
        builder.Property(d => d.ClaimId);

        builder.Property(d => d.EvidenceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.StorageObjectName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.StorageContainer)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.SizeBytes).IsRequired();

        builder.Property(d => d.Sha256)
            .HasMaxLength(64);

        builder.Property(d => d.DocumentDate);
        builder.Property(d => d.Issuer).HasMaxLength(250);

        builder.Property(d => d.VersionNumber).IsRequired();
        builder.Property(d => d.SupersedesDocumentId);

        builder.Property(d => d.UploadedBy).IsRequired();
        builder.Property(d => d.UploadedAtUtc).IsRequired();

        builder.Property(d => d.ScanStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.ScanCompletedAtUtc);
        builder.Property(d => d.RetentionUntil);
        builder.Property(d => d.LegalHold).IsRequired();

        // Optimistic concurrency token (ADR-006)
        builder.Property(d => d.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        // ── Indexes (spec §15.13) ─────────────────────────────────────────────
        // Primary filter: tenant + case + type + status for checklist queries
        builder.HasIndex(d => new { d.TenantId, d.CaseId, d.EvidenceType, d.Status })
            .HasDatabaseName("ix_evidence_documents_tenant_case_type_status");

        // Duplicate checksum detection within a tenant (spec §10.4 invariant 5)
        builder.HasIndex(d => new { d.TenantId, d.Sha256 })
            .HasDatabaseName("uix_evidence_documents_tenant_sha256_available")
            .HasFilter("[status] = 'Available'")
            .IsUnique();

        // Claim evidence lookup
        builder.HasIndex(d => new { d.TenantId, d.ClaimId, d.Status })
            .HasDatabaseName("ix_evidence_documents_tenant_claim_status");

        // Abandoned upload cleanup job — filters by Status + UploadedAtUtc
        builder.HasIndex(d => new { d.TenantId, d.Status, d.UploadedAtUtc })
            .HasDatabaseName("ix_evidence_documents_tenant_status_uploaded");
    }
}
