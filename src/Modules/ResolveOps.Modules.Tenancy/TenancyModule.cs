using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Modules.Tenancy.Features.GetTenant;
using ResolveOps.Modules.Tenancy.Features.TenantSettings;
using ResolveOps.Modules.Tenancy.Features.UpdateTenant;
using ResolveOps.Modules.Tenancy.Features.ListTenantUsers;
using ResolveOps.Modules.Tenancy.Features.InviteUser;
using ResolveOps.Modules.Tenancy.Features.UpdateUserRoles;
using ResolveOps.Modules.Tenancy.Features.DeactivateUser;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Tenancy;

public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        // Register EF configuration assembly
        AppDbContext.AddConfigurationAssembly(typeof(TenancyModule).Assembly);

        // Handlers
        services.AddScoped<GetTenantHandler>();
        services.AddScoped<UpdateTenantHandler>();
        services.AddScoped<TenantSettingsHandlers>();
        services.AddScoped<ListTenantUsersHandler>();
        services.AddScoped<InviteUserHandler>();
        services.AddScoped<UpdateUserRolesHandler>();
        services.AddScoped<DeactivateUserHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<UpdateTenantValidator>(ServiceLifetime.Scoped);
        services.AddValidatorsFromAssemblyContaining<InviteUserValidator>(ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapTenancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        GetTenantEndpoint.MapEndpoint(endpoints);
        UpdateTenantEndpoint.MapEndpoint(endpoints);
        TenantSettingsEndpoint.MapEndpoints(endpoints);
        ListTenantUsersEndpoint.MapEndpoint(endpoints);
        InviteUserEndpoint.MapEndpoint(endpoints);
        UpdateUserRolesEndpoint.MapEndpoint(endpoints);
        DeactivateUserEndpoint.MapEndpoint(endpoints);

        return endpoints;
    }
}
