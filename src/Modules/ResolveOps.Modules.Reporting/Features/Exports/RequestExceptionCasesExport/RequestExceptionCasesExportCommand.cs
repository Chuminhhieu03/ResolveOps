using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestExceptionCasesExport;

public sealed record RequestExceptionCasesExportCommand(
    Guid TenantId,
    Guid UserId,
    Guid? CarrierId = null,
    string? Severity = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
