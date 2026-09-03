using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Identity;

namespace ResolveOps.Modules.Identity.Infrastructure.Persistence;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // IdentityDbContext already sets the base table to "AspNetUsers".
        // We only configure the extra columns added by ApplicationUser.

        builder.Property(u => u.IsSystemAccount)
            .HasColumnName("is_system_account")
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
