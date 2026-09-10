namespace ResolveOps.Domain.Partners;

/// <summary>
/// Customer entity (spec §15.4).
///
/// Invariants:
/// - Code is unique within a tenant (UNIQUE INDEX on tenant_id, code).
/// - Priority drives exception severity calculation.
/// - Version is used for optimistic concurrency.
/// </summary>
public sealed class Customer : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Stable short code, e.g. "ACME". Unique within tenant.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Standard | Premium | Strategic — affects exception severity scoring.</summary>
    public string Priority { get; private set; } = CustomerPriority.Standard;

    /// <summary>IANA timezone for this customer's operational timezone. Optional.</summary>
    public string? DefaultTimezone { get; private set; }

    /// <summary>Active or Inactive.</summary>
    public string Status { get; private set; } = CustomerStatus.Active;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Optimistic concurrency token (spec §15.1).</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // EF Core requires a parameterless constructor.
    private Customer() { }

    public static Customer Create(
        Guid tenantId,
        string code,
        string name,
        string priority,
        string? defaultTimezone,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new Customer
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Priority = priority,
            DefaultTimezone = defaultTimezone,
            Status = CustomerStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }

    public void Update(
        string name,
        string priority,
        string? defaultTimezone,
        TimeProvider timeProvider)
    {
        Name = name.Trim();
        Priority = priority;
        DefaultTimezone = defaultTimezone;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public void Deactivate(TimeProvider timeProvider)
    {
        Status = CustomerStatus.Inactive;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public bool IsActive => Status == CustomerStatus.Active;
}

/// <summary>Valid values for <see cref="Customer.Priority"/>.</summary>
public static class CustomerPriority
{
    public const string Standard = "Standard";
    public const string Premium = "Premium";
    public const string Strategic = "Strategic";

    public static readonly IReadOnlyList<string> All = [Standard, Premium, Strategic];

    public static bool IsValid(string value) =>
        All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Valid values for <see cref="Customer.Status"/>.</summary>
public static class CustomerStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}
