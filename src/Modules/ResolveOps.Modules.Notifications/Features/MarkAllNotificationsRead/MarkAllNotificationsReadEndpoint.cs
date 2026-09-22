using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Features.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/notifications/read-all", async (
            MarkAllNotificationsReadHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            var command = new MarkAllNotificationsReadCommand(userId);

            var result = await handler.HandleAsync(command, cancellationToken);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error.Code, error.Message }));
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .WithName("MarkAllNotificationsRead")
        .WithTags("Notifications");
    }
}
