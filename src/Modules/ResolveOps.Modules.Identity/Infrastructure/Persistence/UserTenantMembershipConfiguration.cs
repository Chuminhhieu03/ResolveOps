using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Identity;

namespace ResolveOps.Modules.Identity.Infrastructure.Persistence;

public sealed class UserTenantMembershipConfiguration : IEntityTypeConfiguration<UserTenantMembership>
{
    public void Configure(EntityTypeBuilder<UserTenantMembership> builder)
    {
        builder.ToTable("user_tenant_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever(); // Application-side Guid.CreateVersion7()

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.RolesJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        // spec §15.3 — UNIQUE(user_id, tenant_id)
        builder.HasIndex(m => new { m.UserId, m.TenantId })
            .IsUnique();

        // Index for tenant-scoped user lookups (list users in a tenant)
        builder.HasIndex(m => new { m.TenantId, m.Status });
    }
}
