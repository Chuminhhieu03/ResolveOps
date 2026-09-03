namespace ResolveOps.Domain;

/// <summary>
/// Strongly-typed user identifier.
/// Prevents accidental substitution of user IDs for other Guid-typed IDs.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId NewId() => new(Guid.CreateVersion7());

    public static UserId Parse(Guid value) => new(value);

    public static bool TryParse(string? input, out UserId result)
    {
        if (Guid.TryParse(input, out var guid))
        {
            result = new UserId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
