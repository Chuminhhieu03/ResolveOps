using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ResolveOps.IntegrationTests.Infrastructure;
using ResolveOps.Persistence;

namespace ResolveOps.IntegrationTests.Persistence;

/// <summary>
/// Verifies that EF Core migrations can be applied successfully against a real
/// SQL Server instance, from a clean database state.
///
/// These tests run against a Testcontainers SQL Server 2022 container and do NOT
/// require any external infrastructure.
/// </summary>
[Collection(nameof(DatabaseFixture))]
public sealed class MigrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public MigrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrateAsync_OnFreshDatabase_Succeeds()
    {
        // Arrange
        await using var context = new AppDbContext(_fixture.CreateDbContextOptions());

        // Act
        var act = () => context.Database.MigrateAsync();

        // Assert — migration must complete without exception
        await act.Should().NotThrowAsync(
            because: "applying migrations to a fresh database must succeed on the first run");
    }

    [Fact]
    public async Task MigrateAsync_WhenAlreadyMigrated_IsIdempotent()
    {
        // Arrange — first migration run
        await using var context1 = new AppDbContext(_fixture.CreateDbContextOptions());
        await context1.Database.MigrateAsync();

        // Act — second migration run on the same database
        await using var context2 = new AppDbContext(_fixture.CreateDbContextOptions());
        var act = () => context2.Database.MigrateAsync();

        // Assert — must succeed without error (EF Core is idempotent by design)
        await act.Should().NotThrowAsync(
            because: "running migrations on an already-migrated database must be safe and idempotent");
    }

    [Fact]
    public async Task Database_AfterMigration_ContainsMigrationsHistoryTable()
    {
        // Arrange
        await using var context = new AppDbContext(_fixture.CreateDbContextOptions());
        await context.Database.MigrateAsync();

        // Act — verify the migrations history table exists
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        // Assert
        appliedMigrations.Should().NotBeEmpty(
            because: "at least the InitialCreate migration must have been applied");
    }
}
