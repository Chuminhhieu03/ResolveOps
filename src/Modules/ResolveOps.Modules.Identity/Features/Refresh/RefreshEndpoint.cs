using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Modules.Identity.Features.Login;

namespace ResolveOps.Modules.Identity.Features.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var handlerDelegate = async (
            RefreshHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "Refresh token is missing.");
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var command = new RefreshCommand(refreshToken, ipAddress, userAgent);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: data =>
                {
                    // Rotate the refresh token cookie
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = data.RefreshTokenExpiresAt
                    };
                    httpContext.Response.Cookies.Append("refresh_token", data.RefreshToken, cookieOptions);

                    return Results.Ok(new LoginResponse(data.AccessToken));
                },
                onFailure: error =>
                {
                    // If refresh fails, clear the cookie
                    httpContext.Response.Cookies.Delete("refresh_token");

                    return Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Authentication Failed",
                        detail: error.Message);
                }
            );
        };

        app.MapPost("/api/identity/refresh", handlerDelegate)
            .WithName("Refresh")
            .WithTags("Identity")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        app.MapPost("/api/auth/refresh", handlerDelegate)
            .WithName("AuthRefresh")
            .WithTags("Identity")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }
}
