using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ResolveOps.Persistence.Conventions;

namespace ResolveOps.Persistence;

/// <summary>
/// Design-time factory used by "dotnet ef migrations" CLI.
/// Reads the connection string from an environment variable or falls back to a
/// local development default — never committed as a real secret.
///
/// Usage:
///   dotnet ef migrations add MigrationName \
///     --project src/BuildingBlocks/ResolveOps.Persistence \
///     --startup-project src/BuildingBlocks/ResolveOps.Persistence
///
/// Override connection string in CI:
///   $env:ConnectionStrings__resolveops = "Server=...;..."
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Allow CI/migration scripts to inject connection string via environment variable.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__resolveops")
            ?? "Server=localhost,1433;Database=ResolveOps;User Id=sa;Password=Dev_Password_1!;TrustServerCertificate=true";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .Options;

        return new AppDbContext(options);
    }
}
