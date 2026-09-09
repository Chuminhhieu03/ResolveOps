using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Infrastructure.Persistence;

public sealed class BusinessCalendarHolidayConfiguration : IEntityTypeConfiguration<BusinessCalendarHoliday>
{
    public void Configure(EntityTypeBuilder<BusinessCalendarHoliday> builder)
    {
        builder.ToTable("business_calendar_holidays");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.TenantId).IsRequired();
        builder.Property(h => h.CalendarId).IsRequired();

        // DateOnly maps to DATE in SQL Server
        builder.Property(h => h.HolidayDate).IsRequired();

        builder.Property(h => h.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(h => h.IsWorkingOverride).IsRequired();

        // spec §15.2 — unique holiday per calendar per date
        builder.HasIndex(h => new { h.TenantId, h.CalendarId, h.HolidayDate })
            .IsUnique()
            .HasDatabaseName("UIX_BusinessCalendarHolidays_TenantCalendarDate");
    }
}
