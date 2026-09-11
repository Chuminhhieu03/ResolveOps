using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Messaging;
using ResolveOps.Modules.Shipments.Features.CancelShipment;
using ResolveOps.Modules.Shipments.Features.CreateShipment;
using ResolveOps.Modules.Shipments.Features.GetShipment;
using ResolveOps.Modules.Shipments.Features.ListShipments;
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
        endpoints.MapEndpoints();
        return endpoints;
    }
}
