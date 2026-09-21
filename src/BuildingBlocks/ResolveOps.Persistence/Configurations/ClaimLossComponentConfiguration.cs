using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Persistence.Configurations;

public class ClaimLossComponentConfiguration : IEntityTypeConfiguration<ClaimLossComponent>
{
    public void Configure(EntityTypeBuilder<ClaimLossComponent> builder)
    {
        builder.ToTable("claim_loss_components");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ComponentType)
            .HasMaxLength(50);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Quantity).HasPrecision(18, 3);
        builder.Property(c => c.UnitAmount).HasPrecision(19, 4);

        // Money value object owned type mapping
        builder.OwnsOne(c => c.Amount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("amount")
                .HasPrecision(19, 4)
                .IsRequired();

            amount.Property(a => a.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });
    }
}
