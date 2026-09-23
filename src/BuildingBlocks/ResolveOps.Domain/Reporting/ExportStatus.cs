using System;
using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Reporting;

public static class ExportStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All = [Pending, Processing, Completed, Failed];

    public static bool IsValid(string? status) =>
        status is not null && All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
