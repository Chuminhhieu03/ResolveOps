using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Messaging;

namespace ResolveOps.Modules.Tracking;

public static class TrackingModule
{
    public static IServiceCollection AddTrackingModule(this IServiceCollection services)
    {
        var assembly = typeof(TrackingModule).Assembly;

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

    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEndpoints();
        return endpoints;
    }
}
