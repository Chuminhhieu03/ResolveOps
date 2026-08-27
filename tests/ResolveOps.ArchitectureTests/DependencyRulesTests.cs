using FluentAssertions;
using NetArchTest.Rules;

namespace ResolveOps.ArchitectureTests;

/// <summary>
/// Architecture tests that enforce modular-monolith dependency rules (ADR-001, ADR-003).
///
/// These rules must hold for the entire lifetime of the project. Failing any test
/// means a module-boundary violation has been introduced. Fix the code, not the test.
/// </summary>
public sealed class DependencyRulesTests
{
    // ─── Assembly names ───────────────────────────────────────────────────────

    private const string DomainAssembly = "ResolveOps.Domain";
    private const string ApplicationAssembly = "ResolveOps.Application";
    private const string PersistenceAssembly = "ResolveOps.Persistence";
    private const string ApiAssembly = "ResolveOps.Api";
    private const string WorkerAssembly = "ResolveOps.Worker";
    private const string AppHostAssembly = "ResolveOps.AppHost";

    private static Types AllTypes() =>
        Types.InAssemblies(
        [
            typeof(ResolveOps.Domain.AssemblyMarker).Assembly,
            typeof(ResolveOps.Application.AssemblyMarker).Assembly,
            typeof(ResolveOps.Persistence.AppDbContext).Assembly,
        ]);

    // ─── Domain isolation ─────────────────────────────────────────────────────

    [Fact]
    public void Domain_ShouldNot_DependOn_Persistence()
    {
        var result = AllTypes()
            .That().ResideInNamespaceStartingWith(DomainAssembly)
            .ShouldNot().HaveDependencyOn(PersistenceAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Persistence — " +
                     "persistence is an implementation detail of the Application layer.");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = AllTypes()
            .That().ResideInNamespaceStartingWith(DomainAssembly)
            .ShouldNot().HaveDependencyOn(ApplicationAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Application — " +
                     "it is the innermost layer with no outbound dependencies.");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_HostProjects()
    {
        var result = AllTypes()
            .That().ResideInNamespaceStartingWith(DomainAssembly)
            .ShouldNot().HaveDependencyOnAny(ApiAssembly, WorkerAssembly, AppHostAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must never depend on host projects.");
    }

    // ─── Application isolation ───────────────────────────────────────────────

    [Fact]
    public void Application_ShouldNot_DependOn_HostProjects()
    {
        var result = AllTypes()
            .That().ResideInNamespaceStartingWith(ApplicationAssembly)
            .ShouldNot().HaveDependencyOnAny(ApiAssembly, WorkerAssembly, AppHostAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not depend on host projects.");
    }

    // ─── Forbidden libraries ──────────────────────────────────────────────────

    [Fact]
    public void NoProject_ShouldUse_AutoMapper()
    {
        var result = AllTypes()
            .ShouldNot().HaveDependencyOn("AutoMapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "AutoMapper is explicitly rejected (ADR-003). Use explicit mapping.");
    }
}
