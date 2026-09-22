using System;
using Microsoft.AspNetCore.Http;

namespace ResolveOps.Security;

/// <summary>
/// Convenient extension methods on <see cref="HttpContext"/> to resolve user and tenant IDs.
/// </summary>
public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext? context) =>
        context?.User.GetUserId() ?? throw new UnauthorizedAccessException("HttpContext is null.");

    public static Guid? TryGetUserId(this HttpContext? context) =>
        context?.User.TryGetUserId();

    public static Guid GetTenantId(this HttpContext? context) =>
        context?.User.GetTenantId() ?? throw new UnauthorizedAccessException("HttpContext is null.");

    public static Guid? TryGetTenantId(this HttpContext? context) =>
        context?.User.TryGetTenantId();
}
