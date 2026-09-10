namespace ResolveOps.Domain.Tenancy;

/// <summary>
/// Tenant-specific configuration stored as a JSON bag for settings that do not
/// require relational integrity (spec §15.2).
///
/// SLA policies, evidence policies, and exception rule definitions use versioned
/// relational models (future phases) — they are NOT stored here.
///
/// This record uses Version for optimistic concurrency on PATCH /tenant/settings.
/// </summary>
public sealed class TenantSettings : IAuditableEntity, IHasConcurrencyStamp
{
    /// <summary>PK and FK to <see cref="Tenant"/> — one-to-one relationship.</summary>
    [System.ComponentModel.DataAnnotations.Key]
    public Guid TenantId { get; private set; }

    /// <summary>
    /// JSON object containing optional, non-relational tenant preferences.
    /// The schema is validated server-side before persistence.
    /// </summary>
    public string SettingsJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Optimistic concurrency token (spec §15.2).</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // EF Core requires a parameterless constructor.
    private TenantSettings() { }

    public static TenantSettings CreateDefault(Guid tenantId, TimeProvider timeProvider)
    {
        return new TenantSettings
        {
            TenantId = tenantId,
            SettingsJson = "{}",
            CreatedAtUtc = timeProvider.GetUtcNow(),
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }

    public void Update(string settingsJson, TimeProvider timeProvider)
    {
        SettingsJson = settingsJson;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}
