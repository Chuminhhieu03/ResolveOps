using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.ListUserExports;

public sealed record ExportItemDto(
    Guid Id,
    string ExportType,
    string Status,
    int? RowCount,
    long? FileSizeBytes,
    string? DownloadUrl,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
