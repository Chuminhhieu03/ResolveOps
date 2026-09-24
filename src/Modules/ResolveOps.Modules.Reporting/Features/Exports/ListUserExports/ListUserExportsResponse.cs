using System.Collections.Generic;

namespace ResolveOps.Modules.Reporting.Features.Exports.ListUserExports;

public sealed record ListUserExportsResponse(
    IReadOnlyList<ExportItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
