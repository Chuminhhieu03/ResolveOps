using System.Diagnostics;

namespace ResolveOps.Api;

/// <summary>
/// Middleware that ensures every HTTP request carries a correlation ID.
///
/// Rules:
/// - Reads X-Correlation-ID from the inbound request header.
/// - If absent, generates a new UUID v7 (time-sortable).
/// - Adds the ID to the current Activity (OpenTelemetry trace baggage).
/// - Enriches the Serilog log scope so every log line in this request carries the ID.
/// - Returns the ID in the X-Correlation-ID response header.
///
/// Structured log field name: CorrelationId (not TenantId; see spec §20.1).
/// </summary>
internal sealed class CorrelationMiddleware
{
    private const string _headerName = "X-Correlation-ID";
    private const string _logPropertyName = "CorrelationId";
    private const string _activityBaggageName = "correlation.id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(_headerName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.CreateVersion7().ToString();

        // Propagate to OpenTelemetry activity baggage.
        var activity = Activity.Current;
        if (activity is not null)
        {
            activity.SetBaggage(_activityBaggageName, correlationId);
            activity.SetTag(_activityBaggageName, correlationId);
        }

        // Add to response so callers can correlate client and server logs.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_headerName] = correlationId;
            return Task.CompletedTask;
        });

        // Enrich all log messages within this request scope.
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [_logPropertyName] = correlationId,
        }))
        {
            await _next(context);
        }
    }
}
