using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="WorkflowTask"/> (spec §15.8).
/// </summary>
public sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("workflow_tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.CaseId).IsRequired();
        builder.Property(t => t.ClaimId);

        builder.Property(t => t.TaskType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(t => t.Description);

        builder.Property(t => t.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.OwnerUserId);
        builder.Property(t => t.OwnerTeamCode).HasMaxLength(50);

        builder.Property(t => t.DueAtUtc);
        builder.Property(t => t.BlockedReason).HasMaxLength(500);
        builder.Property(t => t.CompletionNote);
        builder.Property(t => t.CompletedAtUtc);

        builder.Property(t => t.IsMandatory).IsRequired();
        builder.Property(t => t.WaivedReason).HasMaxLength(500);
        builder.Property(t => t.WaivedByUserId);
        builder.Property(t => t.WaivedAtUtc);

        builder.Property(t => t.CreatedByPolicyId);

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedAtUtc);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);

        // Optimistic concurrency token (ADR-006)
        builder.Property(t => t.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(100)
            .IsRequired();

        // Performance & filter indexes (spec §15.8)
        builder.HasIndex(t => new { t.TenantId, t.CaseId, t.Status })
            .HasDatabaseName("ix_workflow_tasks_tenant_case_status");

        builder.HasIndex(t => new { t.TenantId, t.OwnerUserId, t.Status })
            .HasDatabaseName("ix_workflow_tasks_tenant_owner_status");

        builder.HasIndex(t => new { t.TenantId, t.Status, t.DueAtUtc })
            .HasDatabaseName("ix_workflow_tasks_tenant_status_due");
    }
}
