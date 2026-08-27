using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace ResolveOps.ServiceDefaults;

/// <summary>
/// Shared service-defaults extensions applied identically to API and Worker hosts.
/// Registers Serilog, OpenTelemetry traces/metrics, and health checks.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds shared observability and health-check defaults to any host.
    /// Call before building the application.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureSerilog();
        builder.AddOpenTelemetryDefaults();
        builder.AddDefaultHealthChecks();

        return builder;
    }

    /// <summary>
    /// Maps /health/live and /health/ready endpoints, and the problem-details middleware.
    /// Call after app.Build().
    /// </summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();

        // Liveness: always healthy — signals the process is running.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("live"),
        });

        // Readiness: checks real dependencies (DB, Redis, etc.).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("ready"),
        });

        return app;
    }

    // ─── Serilog ─────────────────────────────────────────────────────────────

    private static IHostApplicationBuilder ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        // Clear default .NET logging providers; Serilog takes over entirely.
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName();
        });

        return builder;
    }

    // ─── OpenTelemetry ───────────────────────────────────────────────────────

    private static IHostApplicationBuilder AddOpenTelemetryDefaults(this IHostApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    // Do not create spans for health-check polling — reduces noise.
                    opts.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }

    // ─── Health checks ───────────────────────────────────────────────────────

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        // Liveness tag: always-passing check — the process is alive.
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Process is running"), tags: ["live"]);

        // SQL Server and Redis readiness checks are registered by the host that wires those
        // dependencies, not here, so that hosts without Redis do not fail unnecessarily.

        return builder;
    }
}
