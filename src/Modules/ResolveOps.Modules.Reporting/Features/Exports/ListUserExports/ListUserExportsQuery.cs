using System;

namespace ResolveOps.Modules.Reporting.Features.Exports.ListUserExports;

public sealed record ListUserExportsQuery(
    Guid TenantId,
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20);
