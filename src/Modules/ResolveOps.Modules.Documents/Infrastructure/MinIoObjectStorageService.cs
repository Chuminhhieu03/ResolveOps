using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResolveOps.Application.Documents;

namespace ResolveOps.Modules.Documents.Infrastructure;

/// <summary>
/// MinIO-backed object storage service using AWSSDK.S3 (spec §13.3, ADR-005).
///
/// Design decisions:
/// - ForcePathStyle=true is required for MinIO (bucket name in URL path, not subdomain).
/// - Permanent public URLs are NEVER returned (spec §10.4, §16.9, §19.6).
/// - Upload presigned URL expires in 15 minutes; download in 30 minutes.
/// - The service is stateless; IAmazonS3 is registered as a singleton.
/// </summary>
public sealed class MinIoObjectStorageService : IObjectStorageService
{
    private static readonly TimeSpan _defaultUploadExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _defaultDownloadExpiry = TimeSpan.FromMinutes(30);

    private static readonly Action<ILogger, string, string, Exception?> _logDeleted =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "ObjectDeleted"),
            "Deleted object {ObjectName} from container {Container}");

    private readonly IAmazonS3 _s3;
    private readonly ObjectStorageOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MinIoObjectStorageService> _logger;

    public MinIoObjectStorageService(
        IAmazonS3 s3,
        IOptions<ObjectStorageOptions> options,
        TimeProvider? timeProvider,
        ILogger<MinIoObjectStorageService> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> GenerateUploadPresignedUrlAsync(
        string container,
        string objectName,
        string contentType,
        TimeSpan expiry,
        CancellationToken ct)
    {
        // Presigned URL generation is synchronous in AWSSDK.S3 and does not contact the server.
        var request = new GetPreSignedUrlRequest
        {
            BucketName = container,
            Key = objectName,
            Verb = HttpVerb.PUT,
            Expires = _timeProvider.GetUtcNow().UtcDateTime.Add(expiry == default ? _defaultUploadExpiry : expiry),
            ContentType = contentType,
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    /// <inheritdoc />
    public Task<string> GenerateDownloadPresignedUrlAsync(
        string container,
        string objectName,
        TimeSpan expiry,
        string? downloadFileName,
        CancellationToken ct)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = container,
            Key = objectName,
            Verb = HttpVerb.GET,
            Expires = _timeProvider.GetUtcNow().UtcDateTime.Add(expiry == default ? _defaultDownloadExpiry : expiry),
        };

        if (!string.IsNullOrWhiteSpace(downloadFileName))
        {
            // RFC 5987 encoding for non-ASCII filenames. Keep it simple for MVP.
            var safe = Uri.EscapeDataString(downloadFileName.Trim());
            request.ResponseHeaderOverrides.ContentDisposition =
                $"attachment; filename*=UTF-8''{safe}";
        }

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    /// <inheritdoc />
    public async Task<bool> ObjectExistsAsync(string container, string objectName, CancellationToken ct)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(container, objectName, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ObjectMetadata?> GetObjectMetadataAsync(string container, string objectName, CancellationToken ct)
    {
        try
        {
            var response = await _s3.GetObjectMetadataAsync(container, objectName, ct).ConfigureAwait(false);
            return new ObjectMetadata(
                ContentType: response.Headers.ContentType ?? "application/octet-stream",
                SizeBytes: response.Headers.ContentLength,
                ETag: response.ETag?.Trim('\"'),
                LastModified: response.LastModified == default ? null : response.LastModified);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadStreamAsync(string container, string objectName, CancellationToken ct)
    {
        var request = new GetObjectRequest
        {
            BucketName = container,
            Key = objectName,
        };

        var response = await _s3.GetObjectAsync(request, ct).ConfigureAwait(false);
        return response.ResponseStream;
    }

    /// <inheritdoc />
    public async Task DeleteObjectAsync(string container, string objectName, CancellationToken ct)
    {
        await _s3.DeleteObjectAsync(container, objectName, ct).ConfigureAwait(false);
        _logDeleted(_logger, objectName, container, null);
    }

    /// <inheritdoc />
    public async Task UploadObjectAsync(
        string container,
        string objectName,
        Stream content,
        string contentType,
        CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = container,
            Key = objectName,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        try
        {
            await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchBucket")
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = container }, ct).ConfigureAwait(false);
            await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);
        }
    }
}
