using Microsoft.EntityFrameworkCore;
using ResolveOps.Persistence.Conventions;

namespace ResolveOps.Persistence;

/// <summary>
/// Root EF Core DbContext for ResolveOps.
///
/// Design decisions (see docs/assumptions.md — Phase 1):
/// - Single shared context for the modular monolith; module-specific entity
///   configurations are discovered automatically via ApplyConfigurationsFromAssembly.
/// - Snake_case naming applied in OnModelCreating by iterating all entities
///   after the model is built, to avoid external package dependency.
/// - SaveChangesAsync is overridden as a hook point for the future outbox/audit
///   interceptor (Phase 5); the base call is unconditional.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Register table-name snake_case convention (spec §15.1).
        configurationBuilder.Conventions.Add(static _ => new SnakeCaseNamingConvention());
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discover all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply snake_case to column names after all configurations have run.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // GetColumnName() returns the already-set name or a default derived from C# property name.
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(SnakeCaseNamingConvention.ToSnakeCase(columnName));
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Hook point for future outbox-pattern and audit-trail interceptors (Phase 5).
    /// Currently delegates directly to the base implementation.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
