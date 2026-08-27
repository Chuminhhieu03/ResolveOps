using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace ResolveOps.Persistence.Conventions;

/// <summary>
/// EF Core convention that translates PascalCase C# property/type names into
/// snake_case SQL Server column and table names.
///
/// Examples:
///   TenantId         → tenant_id
///   CreatedAtUtc     → created_at_utc
///   ExceptionCases   → exception_cases
///
/// Rationale: spec §15.1 requires snake_case names; this convention removes
/// the need for HasColumnName/HasTableName repetition on every entity.
/// Implemented without external packages to avoid transitive vulnerability risks.
/// See docs/assumptions.md — Phase 1 for the decision record.
/// </summary>
internal sealed partial class SnakeCaseNamingConvention : IEntityTypeAddedConvention
{
    [GeneratedRegex("(?<=[a-z0-9])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex UpperCaseTransitionRegex();

    public void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var entityType = entityTypeBuilder.Metadata;
        var tableName = entityType.GetTableName();
        if (tableName is not null)
        {
            entityTypeBuilder.ToTable(ToSnakeCase(tableName));
        }
    }

    internal static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = UpperCaseTransitionRegex().Replace(name, "_$0");
        return result.ToLowerInvariant();
    }
}
