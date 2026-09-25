using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace ResolveOps.Observability;

/// <summary>
/// Serilog enricher that inspects log event properties and redacts credentials,
/// security tokens, financial identifiers, document contents, and PII
/// to enforce Spec §0 Rule 16, §19.9, and §20.2 log sanitization requirements.
/// </summary>
public sealed class PiiSanitizingEnricher : ILogEventEnricher
{
    private const string _redactedPlaceholder = "[REDACTED]";

    private static readonly HashSet<string> _sensitiveKeyWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passphrase",
        "secret",
        "clientsecret",
        "apikey",
        "token",
        "accesstoken",
        "refreshtoken",
        "authorization",
        "bearer",
        "creditcard",
        "cardnumber",
        "cvv",
        "cvc",
        "bankaccount",
        "iban",
        "routingnumber",
        "documentbytes",
        "filebytes",
        "filecontent",
        "rawdocument",
        "ssn"
    };

    private static readonly Regex _jwtRegex = new(
        @"ey[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var keysToSanitize = new List<string>();

        foreach (var property in logEvent.Properties)
        {
            if (IsSensitiveKey(property.Key))
            {
                keysToSanitize.Add(property.Key);
            }
            else if (property.Value is ScalarValue scalar && scalar.Value is string stringValue)
            {
                if (_jwtRegex.IsMatch(stringValue))
                {
                    keysToSanitize.Add(property.Key);
                }
            }
        }

        foreach (var key in keysToSanitize)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(key, new ScalarValue(_redactedPlaceholder)));
        }
    }

    public static bool IsSensitiveKey(string propertyKey)
    {
        if (string.IsNullOrWhiteSpace(propertyKey))
        {
            return false;
        }

        foreach (var keyword in _sensitiveKeyWords)
        {
            if (propertyKey.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return _jwtRegex.Replace(input, _redactedPlaceholder);
    }
}
