using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ResolveOps.Application;

public static class ModuleDiscovery
{
    public static IServiceCollection AddEndpointsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), type);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }

    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        // Simple convention: Any class ending with 'Handler' that is not abstract/interface
        // is registered as its own type automatically with Scoped lifetime.
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            services.AddScoped(type);
        }

        return services;
    }
}
