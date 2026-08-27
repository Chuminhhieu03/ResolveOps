using ResolveOps.Persistence;
using ResolveOps.ServiceDefaults;

// The Worker uses WebApplication builder (not just HostBuilder) so it can expose
// /health/* endpoints for the Aspire dashboard and orchestration readiness probes.

var builder = WebApplication.CreateBuilder(args);

// ── Service defaults (Serilog, OpenTelemetry, health checks) ─────────────
builder.AddServiceDefaults();

// ── Problem Details ───────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Persistence: EF Core via Aspire integration ───────────────────────────
builder.AddSqlServerDbContext<AppDbContext>("resolveops");

// ── Cache: Redis via Aspire integration ──────────────────────────────────
builder.AddRedisClient("redis");

// ── SQL Server readiness health check ────────────────────────────────────
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);

// ─────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseServiceDefaults(); // maps /health/live and /health/ready

app.Run();
