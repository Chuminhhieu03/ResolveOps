namespace ResolveOps.Modules.Documents.Infrastructure;

/// <summary>
/// Configuration options for the MinIO / S3-compatible object storage backend (spec §13.3, ADR-005).
/// Bound from the "ObjectStorage" configuration section.
/// </summary>
public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>MinIO service URL, e.g. "http://localhost:9000".</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>S3 access key (MinIO root user or IAM-equivalent).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>S3 secret key. Never log or expose this value.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Default bucket name for evidence documents.
    /// Bucket is expected to be private; all access via presigned URLs.
    /// </summary>
    public string BucketName { get; set; } = "resolveops-evidence";

    /// <summary>Region string passed to AWSSDK.S3. MinIO ignores this value but it is required by the SDK.</summary>
    public string Region { get; set; } = "us-east-1";
}
