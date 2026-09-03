namespace ResolveOps.Domain;

/// <summary>
/// Interface for entities that track their creation and last update timestamps.
/// Used by EF Core interceptors or manual property updates.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset UpdatedAtUtc { get; }
}
