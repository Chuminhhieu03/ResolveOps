namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Baseline severity levels for exception cases (spec §7.2).
/// </summary>
public static class ExceptionSeverity
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly IReadOnlyList<string> All = [Low, Medium, High, Critical];

    public static bool IsValid(string? severity) =>
        !string.IsNullOrWhiteSpace(severity) && All.Contains(severity, StringComparer.OrdinalIgnoreCase);
}
