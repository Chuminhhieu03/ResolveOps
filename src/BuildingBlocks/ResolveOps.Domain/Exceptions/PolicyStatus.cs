namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Lifecycle status of an exception policy version (spec §15.7, §16.11).
/// </summary>
public static class PolicyStatus
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Retired = "Retired";

    public static readonly IReadOnlyList<string> All = [Draft, Active, Retired];
}
