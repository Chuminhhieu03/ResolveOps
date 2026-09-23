using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var assembly = typeof(ReportingModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext
        AppDbContext.AddConfigurationAssembly(assembly);

        // Register validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-register handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-register endpoints
        services.AddEndpointsFromAssembly(assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(ReportingModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
