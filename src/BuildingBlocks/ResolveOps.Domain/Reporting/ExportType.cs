using System;
using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Reporting;

public static class ExportType
{
    public const string ExceptionCases = "ExceptionCases";
    public const string Claims = "Claims";
    public const string CarrierScorecards = "CarrierScorecards";

    public static readonly IReadOnlyList<string> All = [ExceptionCases, Claims, CarrierScorecards];

    public static bool IsValid(string? type) =>
        type is not null && All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
