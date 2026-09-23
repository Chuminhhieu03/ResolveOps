using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestExceptionCasesExport;

public sealed record RequestExceptionCasesExportResponse(
    Guid ExportId,
    string Status,
    string PollUri,
    DateTimeOffset CreatedAtUtc);
