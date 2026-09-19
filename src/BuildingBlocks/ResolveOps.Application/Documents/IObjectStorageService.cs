namespace ResolveOps.Application.Documents;

/// <summary>
/// Object storage service abstraction (spec §13.3, ADR-005).
///
/// Implemented by MinIoObjectStorageService using AWSSDK.S3 with MinIO endpoint.
/// Permanent public URLs are never returned — all access is through presigned URLs
/// or authorized streams (spec §10.4 invariant 3, §16.9).
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Generates a short-lived presigned PUT URL for direct client upload.
    /// The URL expires after <paramref name="expiry"/> and must not be cached or logged.
    /// </summary>
    Task<string> GenerateUploadPresignedUrlAsync(
        string container,
        string objectName,
        string contentType,
        TimeSpan expiry,
        CancellationToken ct);

    /// <summary>
    /// Generates a short-lived presigned GET URL for authorized download.
    /// Optionally sets a Content-Disposition header with <paramref name="downloadFileName"/>.
    /// The URL expires after <paramref name="expiry"/> and must not be cached or logged.
    /// </summary>
    Task<string> GenerateDownloadPresignedUrlAsync(
        string container,
        string objectName,
        TimeSpan expiry,
        string? downloadFileName,
        CancellationToken ct);

    /// <summary>
    /// Returns true if the object exists in the specified container.
    /// </summary>
    Task<bool> ObjectExistsAsync(string container, string objectName, CancellationToken ct);

    /// <summary>
    /// Returns metadata for the specified object (content type, size, ETag, last modified).
    /// Returns null when the object does not exist.
    /// </summary>
    Task<ObjectMetadata?> GetObjectMetadataAsync(string container, string objectName, CancellationToken ct);

    /// <summary>
    /// Opens a readable stream for the object. Caller must dispose the stream.
    /// Used by DocumentProcessingWorker to stream-hash and scan without loading into memory.
    /// </summary>
    Task<Stream> OpenReadStreamAsync(string container, string objectName, CancellationToken ct);

    /// <summary>
    /// Permanently deletes an object from storage.
    /// Called only for physical cleanup (e.g., Phase 16 storage housekeeping).
    /// Application-level removal uses DocumentStatus.Removed (soft removal in DB).
    /// </summary>
    Task DeleteObjectAsync(string container, string objectName, CancellationToken ct);
}
