using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain.Identity;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Persistence.Conventions;

namespace ResolveOps.Persistence;

/// <summary>
/// Root EF Core DbContext for ResolveOps.
///
/// Design decisions (see docs/assumptions.md — Phase 1 and Phase 2):
/// - Extends IdentityDbContext so ASP.NET Core Identity tables are managed here.
/// - Single shared context for the modular monolith; module EF configurations
///   are discovered via <see cref="AddConfigurationAssembly"/> at module startup.
/// - Snake_case naming applied after all configurations have run.
/// - SaveChangesAsync is overridden as a hook point for outbox/audit interceptors (Phase 5).
///
/// Tenant-aware query filters (spec §19.4):
/// - Entities with a TenantId get a global query filter using the current request's tenantId.
/// - When tenantId is null (migrations, admin jobs, seeding), no filter is applied.
///
/// Dependency pattern (see docs/assumptions.md — A-012):
/// - Persistence references module projects to declare DbSet&lt;T&gt; properties and for
///   the EF design-time factory (dotnet ef migrations add). This is the accepted
///   trade-off for a modular monolith with one shared DbContext.
/// - Module projects reference EF Core directly, NOT Persistence — preventing circular deps.
/// - Module DI registrations call AddConfigurationAssembly() to register their EF configs.
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    // ── Module assembly registry ──────────────────────────────────────────────
    private static readonly List<System.Reflection.Assembly> _configurationAssemblies = [];

    /// <summary>
    /// Called by module DI registrations (e.g. IdentityModule.Register, TenancyModule.Register)
    /// to register their EF configuration assemblies before the first DbContext is built.
    /// </summary>
    public static void AddConfigurationAssembly(System.Reflection.Assembly assembly)
    {
        lock (_configurationAssemblies)
        {
            if (!_configurationAssemblies.Contains(assembly))
            {
                _configurationAssemblies.Add(assembly);
            }
        }
    }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();
    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    // ── Tenant filter ─────────────────────────────────────────────────────────
    private readonly Guid? _currentTenantId;

    /// <summary>Primary constructor — no tenant filter (design-time factory, migrations, seeding).</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Request-scoped constructor — tenant filter active.
    /// Registered via factory in DI so each HTTP request gets the correct tenant ID
    /// from the resolved ITenantContext.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, Guid? tenantId)
        : base(options)
    {
        _currentTenantId = tenantId;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(static _ => new SnakeCaseNamingConvention());
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IdentityDbContext must run first to configure its own tables.
        base.OnModelCreating(builder);

        // Scan this assembly (in case any configurations live here directly).
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply configurations from each module assembly registered at startup.
        List<System.Reflection.Assembly> snapshot;
        lock (_configurationAssemblies)
        {
            snapshot = [.. _configurationAssemblies];
        }

        foreach (var asm in snapshot)
        {
            builder.ApplyConfigurationsFromAssembly(asm);
        }

        // ── Tenant query filters (spec §19.4 — defense in depth) ──────────────
        builder.Entity<UserTenantMembership>()
            .HasQueryFilter(m => _currentTenantId == null || m.TenantId == _currentTenantId);

        builder.Entity<RefreshTokenSession>()
            .HasQueryFilter(s => _currentTenantId == null || s.TenantId == _currentTenantId);

        // Tenant is the isolation root — no tenant filter on it.

        // ── Snake_case column names ────────────────────────────────────────────
        // Applied last so configurations' explicit column names are preserved.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(SnakeCaseNamingConvention.ToSnakeCase(columnName));
                }
            }
        }
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
