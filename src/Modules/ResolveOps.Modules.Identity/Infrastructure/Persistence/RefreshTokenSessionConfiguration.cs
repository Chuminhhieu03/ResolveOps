using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResolveOps.Domain.Identity;

namespace ResolveOps.Modules.Identity.Infrastructure.Persistence;

public sealed class RefreshTokenSessionConfiguration : IEntityTypeConfiguration<RefreshTokenSession>
{
    public void Configure(EntityTypeBuilder<RefreshTokenSession> builder)
    {
        builder.ToTable("refresh_token_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();

        builder.Property(s => s.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.FamilyId).IsRequired();

        builder.Property(s => s.IssuedAtUtc).IsRequired();
        builder.Property(s => s.ExpiresAtUtc).IsRequired();
        builder.Property(s => s.RevokedAtUtc);
        builder.Property(s => s.ReplacedById);

        builder.Property(s => s.CreatedIpHash).HasMaxLength(200);
        builder.Property(s => s.UserAgentHash).HasMaxLength(200);

        builder.Property(s => s.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // Lookup by token hash (login/refresh flow)
        builder.HasIndex(s => s.TokenHash).IsUnique();

        // Revoke all sessions in a family
        builder.HasIndex(s => new { s.UserId, s.FamilyId });

        // Cleanup job: find expired sessions per tenant
        builder.HasIndex(s => new { s.TenantId, s.ExpiresAtUtc });
    }
}
