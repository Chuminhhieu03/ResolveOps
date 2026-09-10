using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Identity.Features.Login;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/login", async (
            LoginCommand command,
            LoginHandler handler,
            IValidator<LoginCommand> validator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Capture IP and UserAgent from HttpContext
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var enrichedCommand = command with
            {
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var result = await handler.HandleAsync(enrichedCommand, cancellationToken);

            return result.Match(
                onSuccess: data =>
                {
                    // Set refresh token in HttpOnly, Secure cookie
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
                onFailure: error => error.ToProblemDetails()
            );
        })
        .WithName("Login")
        .WithTags("Identity")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();
    }
}
