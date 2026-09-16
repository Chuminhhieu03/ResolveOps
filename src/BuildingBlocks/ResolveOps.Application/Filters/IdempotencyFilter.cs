using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application.Idempotency;
using ResolveOps.Domain;
using ResolveOps.Domain.Messaging;
using ResolveOps.Security;

namespace ResolveOps.Application.Filters;

public sealed class IdempotencyFilter : IEndpointFilter
{
    private readonly string _scope;

    public IdempotencyFilter(string scope)
    {
        _scope = scope;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var rawKey) ||
            string.IsNullOrWhiteSpace(rawKey))
        {
            return await next(context);
        }

        var idempotencyKey = rawKey.ToString().Trim();
        var tenantContext = httpContext.RequestServices.GetService<ITenantContext>();
        var idempotencyStore = httpContext.RequestServices.GetService<IIdempotencyStore>();
        var timeProvider = httpContext.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;

        if (tenantContext is null || idempotencyStore is null)
        {
            return await next(context);
        }

        Guid tenantId;
        try
        {
            tenantId = tenantContext.TenantId.Value;
        }
        catch (InvalidOperationException)
        {
            return await next(context);
        }

        // Compute request body hash
        httpContext.Request.EnableBuffering();
        string requestHash;
        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync(httpContext.RequestAborted);
            httpContext.Request.Body.Position = 0;
            requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        }

        var existing = await idempotencyStore.GetRecordAsync(
            tenantId,
            _scope,
            idempotencyKey,
            httpContext.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                var conflictError = DomainError.Failure(
                    "IDEMPOTENCY_KEY_REUSED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The idempotency key '{0}' was previously used with a different request payload.",
                        idempotencyKey));
                return conflictError.ToProblemDetails();
            }

            // Exact replay: return cached response
            if (!string.IsNullOrEmpty(existing.ResponseBody))
            {
                return Results.Content(existing.ResponseBody, "application/json", statusCode: existing.ResponseStatus);
            }

            return Results.StatusCode(existing.ResponseStatus);
        }

        // Execute endpoint
        var result = await next(context);

        // Check if successful (2xx status) to store idempotency record
        var statusCode = 200;
        if (result is IStatusCodeHttpResult statusResult && statusResult.StatusCode.HasValue)
        {
            statusCode = statusResult.StatusCode.Value;
        }

        if (statusCode >= 200 && statusCode < 300)
        {
            string? responseBody = null;
            Guid? resourceId = null;

            if (result is IValueHttpResult valueResult && valueResult.Value is not null)
            {
                responseBody = JsonSerializer.Serialize(valueResult.Value);
                var prop = valueResult.Value.GetType().GetProperty("ShipmentId")
                           ?? valueResult.Value.GetType().GetProperty("Id");
                if (prop != null && prop.PropertyType == typeof(Guid) && prop.GetValue(valueResult.Value) is Guid id)
                {
                    resourceId = id;
                }
            }

            var now = timeProvider.GetUtcNow();
            var record = IdempotencyRecord.Create(
                tenantId,
                _scope,
                idempotencyKey,
                requestHash,
                statusCode,
                responseBody,
                resourceId,
                now,
                now.AddDays(7));

            await idempotencyStore.SaveRecordAsync(record, httpContext.RequestAborted);
        }

        return result;
    }
}

public static class IdempotencyRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder, string scope)
    {
        return builder.AddEndpointFilter(new IdempotencyFilter(scope));
    }
}
