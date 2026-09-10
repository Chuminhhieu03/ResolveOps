using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
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
        var assembly = typeof(IdentityModule).Assembly;

        AppDbContext.AddConfigurationAssembly(assembly);

        // Core services
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<AuditService>();

        // Auto-discovery
        services.AddHandlersFromAssembly(assembly);
        services.AddEndpointsFromAssembly(assembly);
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEndpoints();
        return endpoints;
    }
}
