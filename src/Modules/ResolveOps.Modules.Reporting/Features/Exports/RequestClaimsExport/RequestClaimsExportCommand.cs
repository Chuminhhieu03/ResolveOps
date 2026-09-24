using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestClaimsExport;

public sealed record RequestClaimsExportCommand(
    Guid TenantId,
    Guid UserId,
    Guid? CarrierId = null,
    string? Status = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
