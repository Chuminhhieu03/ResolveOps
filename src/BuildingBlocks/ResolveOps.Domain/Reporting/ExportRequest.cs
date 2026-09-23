using System;

namespace ResolveOps.Domain.Reporting;

public sealed class ExportRequest : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string ExportType { get; private set; } = string.Empty;
    public string Status { get; private set; } = ExportStatus.Pending;
    public string? FilterCriteriaJson { get; private set; }
    public string? Container { get; private set; }
    public string? BlobPath { get; private set; }
    public int? RowCount { get; private set; }
    public long? FileSizeBytes { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private ExportRequest() { } // EF Core

    public static Result<ExportRequest> Create(
        Guid tenantId,
        Guid userId,
        string exportType,
        string? filterCriteriaJson,
        TimeProvider timeProvider)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<ExportRequest>.Failure(new DomainError("INVALID_TENANT", "Tenant ID is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result<ExportRequest>.Failure(new DomainError("INVALID_USER", "User ID is required."));
        }

        if (!Reporting.ExportType.IsValid(exportType))
        {
            return Result<ExportRequest>.Failure(new DomainError("INVALID_EXPORT_TYPE", $"Invalid export type: '{exportType}'."));
        }

        var now = timeProvider.GetUtcNow();

        var request = new ExportRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ExportType = exportType,
            Status = ExportStatus.Pending,
            FilterCriteriaJson = string.IsNullOrWhiteSpace(filterCriteriaJson) ? null : filterCriteriaJson.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return Result<ExportRequest>.Success(request);
    }

    public Result MarkProcessing(TimeProvider timeProvider)
    {
        if (Status != ExportStatus.Pending)
        {
            return Result.Failure(new DomainError("INVALID_STATUS_TRANSITION", $"Cannot transition export from '{Status}' to '{ExportStatus.Processing}'."));
        }

        Status = ExportStatus.Processing;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        return Result.Success();
    }

    public Result MarkCompleted(
        string container,
        string blobPath,
        int rowCount,
        long fileSizeBytes,
        TimeProvider timeProvider)
    {
        if (Status != ExportStatus.Processing && Status != ExportStatus.Pending)
        {
            return Result.Failure(new DomainError("INVALID_STATUS_TRANSITION", $"Cannot transition export from '{Status}' to '{ExportStatus.Completed}'."));
        }

        if (string.IsNullOrWhiteSpace(container))
        {
            return Result.Failure(new DomainError("INVALID_CONTAINER", "Container name is required."));
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return Result.Failure(new DomainError("INVALID_BLOB_PATH", "Blob path is required."));
        }

        if (rowCount < 0)
        {
            return Result.Failure(new DomainError("INVALID_ROW_COUNT", "Row count cannot be negative."));
        }

        if (fileSizeBytes < 0)
        {
            return Result.Failure(new DomainError("INVALID_FILE_SIZE", "File size cannot be negative."));
        }

        var now = timeProvider.GetUtcNow();

        Container = container.Trim();
        BlobPath = blobPath.Trim();
        RowCount = rowCount;
        FileSizeBytes = fileSizeBytes;
        Status = ExportStatus.Completed;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
        ErrorMessage = null;

        return Result.Success();
    }

    public Result MarkFailed(string errorMessage, TimeProvider timeProvider)
    {
        if (Status == ExportStatus.Completed)
        {
            return Result.Failure(new DomainError("INVALID_STATUS_TRANSITION", "Cannot fail an already completed export."));
        }

        var now = timeProvider.GetUtcNow();

        Status = ExportStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown export processing error." : errorMessage.Trim();
        CompletedAtUtc = now;
        UpdatedAtUtc = now;

        return Result.Success();
    }
}
