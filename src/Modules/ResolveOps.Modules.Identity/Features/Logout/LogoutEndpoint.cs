using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ResolveOps.Modules.Identity.Features.Logout;

public static class LogoutEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/logout", async (
            LogoutHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var command = new LogoutCommand(refreshToken);
                await handler.HandleAsync(command, cancellationToken);
            }

            // Always clear the cookie
            httpContext.Response.Cookies.Delete("refresh_token");

            return Results.Ok();
        })
        .WithName("Logout")
        .WithTags("Identity")
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization(); // Requires a valid JWT to logout.
    }
}
