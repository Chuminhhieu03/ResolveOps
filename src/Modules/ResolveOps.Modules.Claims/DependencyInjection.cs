using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application.Claims.Eligibility;
using ResolveOps.Application.Claims.Readiness;
using ResolveOps.Domain.Claims.Eligibility;

namespace ResolveOps.Modules.Claims;

public static class DependencyInjection
{
    public static IServiceCollection AddClaimsModule(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-discover endpoints and handlers would typically happen globally or per-module.
        // We register handlers here if required by the framework, or rely on assembly scanning.

        services.AddScoped<IClaimEligibilityEvaluator, ClaimEligibilityEvaluator>();
        services.AddScoped<IClaimReadinessEvaluator, ClaimReadinessEvaluator>();

        services.AddTransient<Features.CreateDraftClaim.CreateDraftClaimHandler>();
        services.AddTransient<Features.GetClaims.GetClaimsHandler>();
        services.AddTransient<Features.GetClaimById.GetClaimByIdHandler>();
        services.AddTransient<Features.AddLossComponent.AddLossComponentHandler>();
        services.AddTransient<Features.UpdateLossComponent.UpdateLossComponentHandler>();
        services.AddTransient<Features.RemoveLossComponent.RemoveLossComponentHandler>();
        services.AddTransient<Features.CalculateClaimEligibility.CalculateClaimEligibilityHandler>();
        services.AddTransient<Features.GetClaimReadiness.GetClaimReadinessHandler>();

        // Register EF Configurations from this module
        ResolveOps.Persistence.AppDbContext.AddConfigurationAssembly(assembly);

        return services;
    }
}
