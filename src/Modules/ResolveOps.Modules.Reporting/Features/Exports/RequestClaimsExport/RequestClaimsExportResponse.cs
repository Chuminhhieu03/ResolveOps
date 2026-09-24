using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestClaimsExport;

public sealed record RequestClaimsExportResponse(
    Guid ExportId,
    string ExportType,
    string Status,
    string PollUri);
