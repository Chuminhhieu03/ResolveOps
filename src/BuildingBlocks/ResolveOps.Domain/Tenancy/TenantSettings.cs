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
public sealed class TenantSettings
{
    /// <summary>PK and FK to <see cref="Tenant"/> — one-to-one relationship.</summary>
    [System.ComponentModel.DataAnnotations.Key]
    public Guid TenantId { get; private set; }

    /// <summary>
    /// JSON object containing optional, non-relational tenant preferences.
    /// The schema is validated server-side before persistence.
    /// </summary>
    public string SettingsJson { get; private set; } = "{}";

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (spec §15.2).</summary>
    public long Version { get; private set; }

    // EF Core requires a parameterless constructor.
    private TenantSettings() { }

    public static TenantSettings CreateDefault(Guid tenantId, TimeProvider timeProvider)
    {
        return new TenantSettings
        {
            TenantId = tenantId,
            SettingsJson = "{}",
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            Version = 1,
        };
    }

    public void Update(string settingsJson, TimeProvider timeProvider)
    {
        SettingsJson = settingsJson;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }
}
