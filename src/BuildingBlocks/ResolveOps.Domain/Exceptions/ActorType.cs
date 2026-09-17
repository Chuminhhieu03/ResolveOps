namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Actor types that initiate case timeline events (spec §15.7).
/// </summary>
public static class ActorType
{
    public const string System = "System";
    public const string User = "User";
    public const string Carrier = "Carrier";
    public const string Integration = "Integration";
}
