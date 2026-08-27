using ResolveOps.Api;
using ResolveOps.Persistence;
using ResolveOps.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ── Service defaults (Serilog, OpenTelemetry, health checks) ─────────────
builder.AddServiceDefaults();

// ── Problem Details (RFC 9457 error envelope) ────────────────────────────
builder.Services.AddProblemDetails();

// ── OpenAPI ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Persistence: EF Core via Aspire integration ───────────────────────────
// Connection string name "resolveops" matches the Aspire resource name.
builder.AddSqlServerDbContext<AppDbContext>("resolveops");

// ── Cache: Redis via Aspire integration ──────────────────────────────────
builder.AddRedisClient("redis");

// ── SQL Server readiness health check ────────────────────────────────────
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);

// ─────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
app.UseServiceDefaults();          // maps /health/live, /health/ready, exception handler
app.UseMiddleware<CorrelationMiddleware>();
app.UseHttpsRedirection();

// ── OpenAPI (development only) ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ── Endpoints ────────────────────────────────────────────────────────────

// GET /api/version — minimal informational endpoint.
// No authorization required: returns only phase/build metadata, no tenant data.
app.MapGet("/api/version", () => new
{
    Product = "ResolveOps",
    Phase = "1",
    Description = "Runtime, Aspire, database, and observability foundation",
    BuildTimestamp = DateTime.UtcNow.ToString("O"),
})
.WithName("GetVersion")
.WithTags("Meta")
.Produces<object>(StatusCodes.Status200OK);

// ─────────────────────────────────────────────────────────────────────────

app.Run();

// Make Program accessible to integration test WebApplicationFactory.
public partial class Program { }
