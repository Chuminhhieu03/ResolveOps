using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ResolveOps.Api;
using ResolveOps.Api.Infrastructure;
using ResolveOps.Domain.Identity;
using ResolveOps.Modules.Identity;
using ResolveOps.Modules.Partners;
using ResolveOps.Modules.Tenancy;
using ResolveOps.Persistence;
using ResolveOps.Security;
using ResolveOps.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ── Service defaults (Serilog, OpenTelemetry, health checks) ─────────────
builder.AddServiceDefaults();

// ── Problem Details (RFC 9457 error envelope) ────────────────────────────
builder.Services.AddProblemDetails();

// ── OpenAPI ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Persistence: EF Core via Aspire integration ───────────────────────────
builder.AddSqlServerDbContext<AppDbContext>("resolveops");

// ── Cache: Redis via Aspire integration ──────────────────────────────────
builder.AddRedisClient("redis");

// ── Identity Core ─────────────────────────────────────────────────────────
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12; // Spec §25.2 (security constraints)
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>();

// ── Authentication & Authorization (JWT) ──────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SigningKey"] ?? "local-dev-fallback-secret-key-with-enough-bytes-for-sha256";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireActiveTenantMembership, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new TenantMembershipRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, TenantMembershipHandler>();

// ── Modules ───────────────────────────────────────────────────────────────
builder.Services.AddIdentityModule();
builder.Services.AddTenancyModule();
builder.Services.AddPartnersModule();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);

// ─────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
app.UseServiceDefaults();
app.UseMiddleware<CorrelationMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ── OpenAPI (development only) ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Seed local database
    await DevelopmentSeeder.SeedAsync(app.Services);
}

// ── Endpoints ────────────────────────────────────────────────────────────

app.MapGet("/api/version", () => new
{
    Product = "ResolveOps",
    Phase = "2",
    Description = "Tenancy and identity",
    BuildTimestamp = DateTime.UtcNow.ToString("O"),
})
.WithName("GetVersion")
.WithTags("Meta")
.Produces<object>(StatusCodes.Status200OK);

app.MapIdentityEndpoints();
app.MapTenancyEndpoints();
app.MapPartnersEndpoints();

// ─────────────────────────────────────────────────────────────────────────

app.Run();

public partial class Program { }
