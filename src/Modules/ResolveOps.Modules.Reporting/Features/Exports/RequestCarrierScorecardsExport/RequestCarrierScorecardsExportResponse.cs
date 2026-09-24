using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestCarrierScorecardsExport;

public sealed record RequestCarrierScorecardsExportResponse(
    Guid ExportId,
    string ExportType,
    string Status,
    string PollUri);
