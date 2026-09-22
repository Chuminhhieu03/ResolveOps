using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/notifications/preferences", async (
            [FromBody] UpdateNotificationPreferencesRequest request,
            IValidator<UpdateNotificationPreferencesCommand> validator,
            UpdateNotificationPreferencesHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            var command = new UpdateNotificationPreferencesCommand(userId, request.Preferences ?? []);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            return result.Match(
                success => Results.Ok(success),
                error => error.Code == "CANNOT_DISABLE_MANDATORY_NOTIFICATION"
                    ? Results.Conflict(new { error.Code, error.Message })
                    : Results.BadRequest(new { error.Code, error.Message }));
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .WithName("UpdateNotificationPreferences")
        .WithTags("Notifications");
    }
}
