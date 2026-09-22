using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Features.GetNotificationPreferences;

public sealed class GetNotificationPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/notifications/preferences", async (
            GetNotificationPreferencesHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            var query = new GetNotificationPreferencesQuery(userId);

            var result = await handler.HandleAsync(query, cancellationToken);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error.Code, error.Message }));
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .WithName("GetNotificationPreferences")
        .WithTags("Notifications");
    }
}
