using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Persistence;
using Testcontainers.MsSql;

namespace ResolveOps.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit async lifetime fixture that provisions a real SQL Server instance via
/// Testcontainers for each test class that uses it.
///
/// Usage: implement IClassFixture&lt;DatabaseFixture&gt; in your test class.
///
/// Connection string is available via <see cref="ConnectionString"/>.
/// A pre-built <see cref="DbContextOptions{AppDbContext}"/> is available via
/// <see cref="CreateDbContextOptions"/>.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("IntTest_Pass_1!")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public DbContextOptions<AppDbContext> CreateDbContextOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .Options;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
