namespace ResolveOps.Domain.Identity;

/// <summary>
/// Represents a user's membership in a specific tenant.
///
/// A single <see cref="ApplicationUser"/> can belong to multiple tenants through
/// separate membership records. The membership record governs:
/// - activation status within the tenant;
/// - the roles the user holds in that tenant;
/// - when the membership was established.
///
/// TenantId is never derived from request body — it is always resolved from
/// the authenticated token's membership claim (spec §10.6 rule 2).
/// </summary>
public sealed class UserTenantMembership
{
    public Guid Id { get; private set; }

    /// <summary>The tenant this membership belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The user this membership belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Membership lifecycle status: Active, Inactive, Pending.</summary>
    public string Status { get; private set; } = MembershipStatus.Active;

    /// <summary>
    /// JSON-serialized array of role names the user holds in this tenant.
    /// Deserialized server-side; never exposed as raw JSON to the API caller.
    /// </summary>
    public string RolesJson { get; private set; } = "[]";

    public DateTimeOffset CreatedAtUtc { get; private set; }

    // EF Core requires a parameterless constructor.
    private UserTenantMembership() { }

    public static UserTenantMembership Create(
        Guid tenantId,
        Guid userId,
        IEnumerable<string> roles,
        TimeProvider timeProvider)
    {
        var roleList = roles.ToList();
        return new UserTenantMembership
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            Status = MembershipStatus.Active,
            RolesJson = System.Text.Json.JsonSerializer.Serialize(roleList),
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
    }

    /// <summary>Returns the deserialized list of role names.</summary>
    public IReadOnlyList<string> GetRoles() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(RolesJson) ?? [];

    public void UpdateRoles(IEnumerable<string> roles)
    {
        RolesJson = System.Text.Json.JsonSerializer.Serialize(roles.ToList());
    }

    public void Deactivate() => Status = MembershipStatus.Inactive;

    public void Activate() => Status = MembershipStatus.Active;

    public bool IsActive => Status == MembershipStatus.Active;
}

/// <summary>Valid values for <see cref="UserTenantMembership.Status"/>.</summary>
public static class MembershipStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Pending = "Pending";
}
