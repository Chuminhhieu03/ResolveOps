namespace ResolveOps.Domain;

/// <summary>
/// Strongly-typed tenant identifier.
/// Using a record struct avoids accidental confusion with other Guid-typed IDs.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    public static TenantId NewId() => new(Guid.CreateVersion7());

    public static TenantId Parse(Guid value) => new(value);

    public static bool TryParse(string? input, out TenantId result)
    {
        if (Guid.TryParse(input, out var guid))
        {
            result = new TenantId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
