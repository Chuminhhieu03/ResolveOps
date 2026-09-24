using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestCarrierScorecardsExport;

public sealed record RequestCarrierScorecardsExportCommand(
    Guid TenantId,
    Guid UserId,
    Guid? CarrierId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
