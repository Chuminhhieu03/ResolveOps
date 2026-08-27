// Phase 1: Aspire AppHost — local development orchestration.
// Run with: dotnet run --project src/AppHost/ResolveOps.AppHost
// Requires Docker to be running for SQL Server and Redis containers.

var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure resources ──────────────────────────────────────────────

var sqlServer = builder
    .AddSqlServer("sqlserver")
    .AddDatabase("resolveops");

var redis = builder.AddRedis("redis");

// ── Application resources ─────────────────────────────────────────────────

builder.AddProject<Projects.ResolveOps_Api>("api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WaitFor(sqlServer)
    .WaitFor(redis);

builder.AddProject<Projects.ResolveOps_Worker>("worker")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WaitFor(sqlServer)
    .WaitFor(redis);

// ─────────────────────────────────────────────────────────────────────────

builder.Build().Run();

// ── Manual Project Metadata (bypasses Aspire 9.x source generator issues on .NET 10 SDK) ──
namespace Projects
{
    public class ResolveOps_Api : Aspire.Hosting.ApplicationModel.IProjectMetadata
    {
        public string ProjectPath => @"..\..\Hosts\ResolveOps.Api\ResolveOps.Api.csproj";
    }

    public class ResolveOps_Worker : Aspire.Hosting.ApplicationModel.IProjectMetadata
    {
        public string ProjectPath => @"..\..\Hosts\ResolveOps.Worker\ResolveOps.Worker.csproj";
    }
}
