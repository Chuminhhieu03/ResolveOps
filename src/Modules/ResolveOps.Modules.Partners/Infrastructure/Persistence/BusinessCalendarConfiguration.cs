using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Infrastructure.Persistence;

public sealed class BusinessCalendarConfiguration : IEntityTypeConfiguration<BusinessCalendar>
{
    public void Configure(EntityTypeBuilder<BusinessCalendar> builder)
    {
        builder.ToTable("business_calendars");

        builder.HasKey(bc => bc.Id);
        builder.Property(bc => bc.Id).ValueGeneratedNever();

        builder.Property(bc => bc.TenantId).IsRequired();

        builder.Property(bc => bc.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(bc => bc.Timezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(bc => bc.WorkingDaysMask).IsRequired();

        // TimeOnly maps to TIME in SQL Server
        builder.Property(bc => bc.WorkingStart).IsRequired();
        builder.Property(bc => bc.WorkingEnd).IsRequired();

        builder.Property(bc => bc.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(bc => bc.CreatedAtUtc).IsRequired();
        builder.Property(bc => bc.UpdatedAtUtc).IsRequired();

        builder.Property(bc => bc.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // spec §15.2 — name unique within tenant
        builder.HasIndex(bc => new { bc.TenantId, bc.Name })
            .IsUnique()
            .HasDatabaseName("UIX_BusinessCalendars_TenantName");

        // Navigation to holidays
        builder.HasMany(bc => bc.Holidays)
            .WithOne()
            .HasForeignKey(h => h.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
