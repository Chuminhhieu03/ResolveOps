using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Modules.Tenancy.Features.DeactivateUser;
using ResolveOps.Modules.Tenancy.Features.GetTenant;
using ResolveOps.Modules.Tenancy.Features.InviteUser;
using ResolveOps.Modules.Tenancy.Features.ListTenantUsers;
using ResolveOps.Modules.Tenancy.Features.TenantSettings;
using ResolveOps.Modules.Tenancy.Features.UpdateTenant;
using ResolveOps.Modules.Tenancy.Features.UpdateUserRoles;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Tenancy;

public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        var assembly = typeof(TenancyModule).Assembly;

        AppDbContext.AddConfigurationAssembly(assembly);

        // Auto-discovery
        services.AddHandlersFromAssembly(assembly);
        services.AddEndpointsFromAssembly(assembly);
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapTenancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(TenancyModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
