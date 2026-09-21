using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Application.Claims.Eligibility;
using ResolveOps.Application.Claims.Readiness;
using ResolveOps.Domain.Claims.Eligibility;

namespace ResolveOps.Modules.Claims;

public static class ClaimsModule
{
    public static IServiceCollection AddClaimsModule(this IServiceCollection services)
    {
        var assembly = typeof(ClaimsModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext
        ResolveOps.Persistence.AppDbContext.AddConfigurationAssembly(assembly);

        // Register Validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-Register Handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-Register Endpoints
        services.AddEndpointsFromAssembly(assembly);

        services.AddScoped<IClaimEligibilityEvaluator, ClaimEligibilityEvaluator>();
        services.AddScoped<IClaimReadinessEvaluator, ClaimReadinessEvaluator>();

        return services;
    }

    public static IEndpointRouteBuilder MapClaimsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(ClaimsModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
