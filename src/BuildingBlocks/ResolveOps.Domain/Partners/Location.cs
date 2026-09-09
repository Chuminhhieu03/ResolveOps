namespace ResolveOps.Domain.Partners;

/// <summary>
/// Location entity (spec §15.4).
///
/// Locations represent warehouses, hubs, pickup points, and delivery addresses
/// that can be referenced by shipments, shipment legs, and carriers.
///
/// Invariants:
/// - Code is unique within a tenant.
/// - CountryCode must be ISO 3166-1 alpha-2 (2 uppercase letters).
/// - Timezone must be a valid IANA timezone identifier (validated at application layer).
/// - Latitude/longitude are optional but both must be provided together.
/// </summary>
public sealed class Location
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Stable short code, e.g. "HAN-WH1". Unique within tenant.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "VN", "US".</summary>
    public string CountryCode { get; private set; } = string.Empty;

    /// <summary>IANA timezone identifier, e.g. "Asia/Ho_Chi_Minh".</summary>
    public string Timezone { get; private set; } = "UTC";

    /// <summary>Optional decimal latitude (-90 to 90).</summary>
    public decimal? Latitude { get; private set; }

    /// <summary>Optional decimal longitude (-180 to 180).</summary>
    public decimal? Longitude { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (spec §15.1).</summary>
    public long Version { get; private set; }

    // EF Core requires a parameterless constructor.
    private Location() { }

    public static Location Create(
        Guid tenantId,
        string code,
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string countryCode,
        string timezone,
        decimal? latitude,
        decimal? longitude,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            AddressLine1 = addressLine1?.Trim(),
            AddressLine2 = addressLine2?.Trim(),
            City = city?.Trim(),
            Region = region?.Trim(),
            PostalCode = postalCode?.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            Timezone = timezone,
            Latitude = latitude,
            Longitude = longitude,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void Update(
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string countryCode,
        string timezone,
        decimal? latitude,
        decimal? longitude,
        TimeProvider timeProvider)
    {
        Name = name.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        Region = region?.Trim();
        PostalCode = postalCode?.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Timezone = timezone;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }
}
