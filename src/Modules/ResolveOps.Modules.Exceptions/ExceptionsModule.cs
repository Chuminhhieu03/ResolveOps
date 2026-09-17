using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Messaging;
using ResolveOps.Modules.Exceptions.Services;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions;

public static class ExceptionsModule
{
    public static IServiceCollection AddExceptionsModule(this IServiceCollection services)
    {
        var assembly = typeof(ExceptionsModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext
        AppDbContext.AddConfigurationAssembly(assembly);

        // Register Validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-Register Handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-Register Endpoints
        services.AddEndpointsFromAssembly(assembly);

        // Policy evaluation and domain services
        services.AddScoped<IExceptionFingerprintGenerator, ExceptionFingerprintGenerator>();
        services.AddScoped<ISeverityCalculator, SeverityCalculator>();
        services.AddScoped<IAssignmentEvaluator, AssignmentEvaluator>();
        services.AddScoped<IExceptionPolicyEvaluator, ExceptionPolicyEvaluator>();

        // Register IOutboxWriter scoped so handlers can inject it
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }

    public static IEndpointRouteBuilder MapExceptionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(ExceptionsModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
