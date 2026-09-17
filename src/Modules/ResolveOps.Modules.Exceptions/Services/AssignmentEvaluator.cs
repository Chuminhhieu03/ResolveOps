using System.Text.Json;
using ResolveOps.Modules.Exceptions.Models;

namespace ResolveOps.Modules.Exceptions.Services;

public interface IAssignmentEvaluator
{
    string SelectOwnerTeam(string? assignmentDefinitionJson, string severity, string exceptionType);
}

public sealed class AssignmentEvaluator : IAssignmentEvaluator
{
    public string SelectOwnerTeam(string? assignmentDefinitionJson, string severity, string exceptionType)
    {
        AssignmentDefinition definition = new();
        if (!string.IsNullOrWhiteSpace(assignmentDefinitionJson))
        {
            try
            {
                definition = JsonSerializer.Deserialize<AssignmentDefinition>(assignmentDefinitionJson) ?? new();
            }
            catch (JsonException)
            {
                // Fall back to defaults
            }
        }

        if (definition.SeverityTeamOverrides != null &&
            definition.SeverityTeamOverrides.TryGetValue(severity, out var overrideTeam) &&
            !string.IsNullOrWhiteSpace(overrideTeam))
        {
            return overrideTeam;
        }

        return !string.IsNullOrWhiteSpace(definition.DefaultTeamCode) ? definition.DefaultTeamCode : "OPS-NORTH";
    }
}
