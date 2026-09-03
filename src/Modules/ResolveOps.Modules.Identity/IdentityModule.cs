using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Modules.Identity.Features.Audit;
using ResolveOps.Modules.Identity.Features.Auth;
using ResolveOps.Modules.Identity.Features.ChangePassword;
using ResolveOps.Modules.Identity.Features.GetCurrentUser;
using ResolveOps.Modules.Identity.Features.Login;
using ResolveOps.Modules.Identity.Features.Logout;
using ResolveOps.Modules.Identity.Features.Refresh;
using ResolveOps.Modules.Identity.Infrastructure;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // Register EF configuration assembly with the shared Persistence layer
        AppDbContext.AddConfigurationAssembly(typeof(IdentityModule).Assembly);

        // Core services
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<AuditService>();

        // Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ChangePasswordHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<LoginValidator>(ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        LoginEndpoint.MapEndpoint(endpoints);
        RefreshEndpoint.MapEndpoint(endpoints);
        LogoutEndpoint.MapEndpoint(endpoints);
        GetCurrentUserEndpoint.MapEndpoint(endpoints);
        ChangePasswordEndpoint.MapEndpoint(endpoints);

        return endpoints;
    }
}
