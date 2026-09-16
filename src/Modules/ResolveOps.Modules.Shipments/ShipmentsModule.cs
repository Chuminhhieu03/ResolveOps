using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Messaging;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Shipments;

public static class ShipmentsModule
{
    public static IServiceCollection AddShipmentsModule(this IServiceCollection services)
    {
        var assembly = typeof(ShipmentsModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext
        AppDbContext.AddConfigurationAssembly(assembly);

        // Register Validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-Register Handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-Register Endpoints
        services.AddEndpointsFromAssembly(assembly);

        // Register IOutboxWriter scoped so handlers can inject it
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }

    public static IEndpointRouteBuilder MapShipmentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(ShipmentsModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
