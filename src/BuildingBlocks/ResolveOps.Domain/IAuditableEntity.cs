namespace ResolveOps.Domain;

/// <summary>
/// Interface for entities that track their creation and last update timestamps.
/// Used by EF Core interceptors or manual property updates.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
    string? UpdatedBy { get; set; }
}
