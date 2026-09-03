using Microsoft.AspNetCore.Identity;

namespace ResolveOps.Domain.Identity;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with ResolveOps-specific fields.
///
/// Phase 2 note: Users are global (not per-tenant); tenant association is managed
/// through <see cref="UserTenantMembership"/>. This allows a single user account
/// to belong to multiple tenants — a deliberate design choice (see ADR-008).
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Whether this account is a system/machine account (not a human user).
    /// System accounts cannot log in via the password flow.
    /// </summary>
    public bool IsSystemAccount { get; private set; }

    /// <summary>UTC timestamp when the user record was first created.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>UTC timestamp of the last update to the user record.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    // EF Core requires a parameterless constructor.
    private ApplicationUser() { }

    /// <summary>Creates a new human user account.</summary>
    public static ApplicationUser Create(
        string email,
        string userName,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = userName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
            IsSystemAccount = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
