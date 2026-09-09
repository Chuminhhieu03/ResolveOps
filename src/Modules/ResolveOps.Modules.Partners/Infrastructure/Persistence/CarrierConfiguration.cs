using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Infrastructure.Persistence;

public sealed class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("carriers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.ScacOrExternalCode)
            .HasMaxLength(50);

        builder.Property(c => c.DefaultTimezone)
            .HasMaxLength(100);

        builder.Property(c => c.ContactEmail)
            .HasMaxLength(320);

        builder.Property(c => c.ClaimSubmissionChannel)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.UpdatedAtUtc).IsRequired();

        builder.Property(c => c.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // spec §15.4 — code unique within tenant
        builder.HasIndex(c => new { c.TenantId, c.Code })
            .IsUnique()
            .HasDatabaseName("UIX_Carriers_TenantCode");

        // spec §15.13 — common operational query filter
        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasDatabaseName("IX_Carriers_TenantStatus");
    }
}
