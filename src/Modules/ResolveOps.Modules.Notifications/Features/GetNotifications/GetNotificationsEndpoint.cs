using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Features.GetNotifications;

public sealed class GetNotificationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/notifications", async (
            bool? isRead,
            string? notificationClass,
            int? page,
            int? pageSize,
            IValidator<GetNotificationsQuery> validator,
            GetNotificationsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            var query = new GetNotificationsQuery(
                userId,
                isRead,
                notificationClass,
                page.HasValue && page.Value > 0 ? page.Value : 1,
                pageSize.HasValue && pageSize.Value > 0 ? pageSize.Value : 20);

            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(query, cancellationToken);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error.Code, error.Message }));
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .WithName("GetNotifications")
        .WithTags("Notifications");
    }
}
