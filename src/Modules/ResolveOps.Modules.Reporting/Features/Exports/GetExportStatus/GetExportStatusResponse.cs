using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.GetExportStatus;

public sealed record GetExportStatusResponse(
    Guid ExportId,
    string ExportType,
    string Status,
    int? RowCount,
    long? FileSizeBytes,
    string? DownloadUrl,
    string? ErrorMessage,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc);
