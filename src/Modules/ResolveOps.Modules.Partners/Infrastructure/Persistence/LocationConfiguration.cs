using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Infrastructure.Persistence;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.TenantId).IsRequired();

        builder.Property(l => l.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.AddressLine1)
            .HasMaxLength(250);

        builder.Property(l => l.AddressLine2)
            .HasMaxLength(250);

        builder.Property(l => l.City)
            .HasMaxLength(100);

        builder.Property(l => l.Region)
            .HasMaxLength(100);

        builder.Property(l => l.PostalCode)
            .HasMaxLength(30);

        builder.Property(l => l.CountryCode)
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(l => l.Timezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Latitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(l => l.Longitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(l => l.CreatedAtUtc).IsRequired();
        builder.Property(l => l.UpdatedAtUtc).IsRequired();

        builder.Property(l => l.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // spec §15.4 — code unique within tenant
        builder.HasIndex(l => new { l.TenantId, l.Code })
            .IsUnique()
            .HasDatabaseName("UIX_Locations_TenantCode");
    }
}
