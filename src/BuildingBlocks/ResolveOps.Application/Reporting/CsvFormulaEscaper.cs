using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResolveOps.Application.Reporting;

/// <summary>
/// Provides CSV formula injection protection (spec §11, §19.9, §22.7) and RFC 4180 compliant CSV formatting.
///
/// Spreadsheet applications (Excel, LibreOffice Calc, etc.) interpret cells starting with
/// '=', '+', '-', '@', '\t', or '\r' as formulas or DDE commands. Prepending a single quote
/// forces spreadsheet applications to treat the cell contents strictly as plain text.
/// </summary>
public static class CsvFormulaEscaper
{
    private static readonly char[] _formulaTriggerChars = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] _charactersRequiringQuotes = [',', '"', '\r', '\n', '\''];

    /// <summary>
    /// Escapes a raw string to neutralize CSV formula injection risks.
    /// If the text starts with '=', '+', '-', '@', '\t', or '\r', it prepends a single quote (').
    /// </summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmedLeading = value.TrimStart(' ');
        if (trimmedLeading.Length > 0 && _formulaTriggerChars.Contains(trimmedLeading[0]))
        {
            return "'" + value;
        }

        return value;
    }

    /// <summary>
    /// Escapes formula injection risks and wraps the field in quotes if necessary per RFC 4180.
    /// </summary>
    public static string EscapeField(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var escaped = Escape(value);

        var requiresQuotes = escaped.IndexOfAny(_charactersRequiringQuotes) >= 0 ||
                             escaped.StartsWith(' ') ||
                             escaped.EndsWith(' ');

        if (requiresQuotes)
        {
            return $"\"{escaped.Replace("\"", "\"\"")}\"";
        }

        return escaped;
    }

    /// <summary>
    /// Formats a complete CSV row by escaping and joining all fields with commas.
    /// </summary>
    public static string FormatRow(IEnumerable<string?> fields)
    {
        var escapedFields = fields.Select(EscapeField);
        return string.Join(",", escapedFields);
    }
}
