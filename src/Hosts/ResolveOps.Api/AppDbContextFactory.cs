using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ResolveOps.Modules.Identity;
using ResolveOps.Modules.Tenancy;
using ResolveOps.Persistence;

namespace ResolveOps.Api;

/// <summary>
/// Design-time factory for EF Core Migrations.
/// This ensures module assemblies are registered before the model is built during 'dotnet ef'.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Explicitly register module assemblies for design-time model creation
        AppDbContext.AddConfigurationAssembly(typeof(IdentityModule).Assembly);
        AppDbContext.AddConfigurationAssembly(typeof(TenancyModule).Assembly);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=ResolveOps_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
