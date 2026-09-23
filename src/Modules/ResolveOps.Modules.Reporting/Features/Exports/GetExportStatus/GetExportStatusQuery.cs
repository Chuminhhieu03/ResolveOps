using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.GetExportStatus;

public sealed record GetExportStatusQuery(
    Guid TenantId,
    Guid UserId,
    Guid ExportId);
