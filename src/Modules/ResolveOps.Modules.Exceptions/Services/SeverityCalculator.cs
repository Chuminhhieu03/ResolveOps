using System.Text.Json;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Modules.Exceptions.Models;

namespace ResolveOps.Modules.Exceptions.Services;

public record SeverityCalculationResult(string Severity, int SeverityScore);

public interface ISeverityCalculator
{
    SeverityCalculationResult Calculate(
        string? severityDefinitionJson,
        decimal? declaredValue,
        int? delayMinutes,
        string? initialSeverityHint = null);
}

public sealed class SeverityCalculator : ISeverityCalculator
{
    public SeverityCalculationResult Calculate(
        string? severityDefinitionJson,
        decimal? declaredValue,
        int? delayMinutes,
        string? initialSeverityHint = null)
    {
        SeverityDefinition definition = new();
        if (!string.IsNullOrWhiteSpace(severityDefinitionJson))
        {
            try
            {
                definition = JsonSerializer.Deserialize<SeverityDefinition>(severityDefinitionJson) ?? new();
            }
            catch (JsonException)
            {
                // Fall back to defaults
            }
        }

        // Check if initial hint is Critical
        if (string.Equals(initialSeverityHint, ExceptionSeverity.Critical, StringComparison.OrdinalIgnoreCase))
        {
            return new SeverityCalculationResult(ExceptionSeverity.Critical, 100);
        }

        var value = declaredValue ?? 0m;
        var delay = delayMinutes ?? 0;

        // Score model from 0 to 100
        var score = 30; // base score

        if (value >= definition.CriticalValueThreshold || delay >= definition.CriticalDelayMinutesThreshold)
        {
            return new SeverityCalculationResult(ExceptionSeverity.Critical, 90);
        }

        if (value >= definition.HighValueThreshold || delay >= definition.HighDelayMinutesThreshold)
        {
            return new SeverityCalculationResult(ExceptionSeverity.High, 70);
        }

        if (value > 1000m || delay > 60)
        {
            return new SeverityCalculationResult(ExceptionSeverity.Medium, 50);
        }

        if (string.Equals(initialSeverityHint, ExceptionSeverity.High, StringComparison.OrdinalIgnoreCase))
        {
            return new SeverityCalculationResult(ExceptionSeverity.High, 70);
        }

        var defaultSeverity = ExceptionSeverity.IsValid(definition.DefaultSeverity)
            ? definition.DefaultSeverity
            : ExceptionSeverity.Medium;

        return new SeverityCalculationResult(defaultSeverity, score);
    }
}
