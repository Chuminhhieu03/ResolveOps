namespace ResolveOps.Application.Documents;

/// <summary>
/// Metadata returned when inspecting an object in object storage (spec §13.3).
/// </summary>
public sealed record ObjectMetadata(
    string ContentType,
    long SizeBytes,
    string? ETag,
    DateTimeOffset? LastModified);
