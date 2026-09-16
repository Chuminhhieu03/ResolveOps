namespace ResolveOps.Domain.Tenancy;

/// <summary>
/// Tenant aggregate root.
///
/// A tenant represents an isolated organization in ResolveOps. All business data
/// is scoped to a tenant (spec §10.6). The Tenant itself is not tenant-scoped —
/// it is the top-level isolation boundary.
///
/// Invariants:
/// - Code is unique across all tenants (used as a stable external identifier).
/// - Version is used for optimistic concurrency on PATCH operations.
/// - DefaultCurrency must be a valid ISO 4217 code.
/// - DefaultTimezone must be a valid IANA timezone identifier.
/// </summary>
public sealed class Tenant : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }

    /// <summary>Short stable identifier, e.g. "acme-logistics". Unique globally.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Lifecycle status: Active, Suspended, Provisioning.</summary>
    public string Status { get; private set; } = TenantStatus.Active;

    /// <summary>IANA timezone identifier, e.g. "Asia/Ho_Chi_Minh".</summary>
    public string DefaultTimezone { get; private set; } = "UTC";

    /// <summary>ISO 4217 currency code, e.g. "VND", "USD".</summary>
    public string DefaultCurrency { get; private set; } = "USD";

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Optimistic concurrency token — updated on every change.</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // EF Core requires a parameterless constructor.
    private Tenant() { }

    public static Tenant Create(
        string code,
        string name,
        string defaultTimezone,
        string defaultCurrency,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new Tenant
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = name.Trim(),
            DefaultTimezone = defaultTimezone,
            DefaultCurrency = defaultCurrency.ToUpperInvariant(),
            Status = TenantStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }

    public void Update(
        string name,
        string defaultTimezone,
        string defaultCurrency,
        TimeProvider timeProvider)
    {
        Name = name.Trim();
        DefaultTimezone = defaultTimezone;
        DefaultCurrency = defaultCurrency.ToUpperInvariant();
    }

    public void Suspend(TimeProvider? timeProvider = null)
    {
        Status = TenantStatus.Suspended;
    }

    public void Activate(TimeProvider? timeProvider = null)
    {
        Status = TenantStatus.Active;
    }

    public bool IsActive => Status == TenantStatus.Active;
}

/// <summary>Valid values for <see cref="Tenant.Status"/>.</summary>
public static class TenantStatus
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Provisioning = "Provisioning";
}
