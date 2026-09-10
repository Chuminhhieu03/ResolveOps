using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Modules.Partners.Features.ActivateCarrier;
using ResolveOps.Modules.Partners.Features.AddHoliday;
using ResolveOps.Modules.Partners.Features.CreateBusinessCalendar;
using ResolveOps.Modules.Partners.Features.CreateCarrier;
using ResolveOps.Modules.Partners.Features.CreateCustomer;
using ResolveOps.Modules.Partners.Features.CreateLocation;
using ResolveOps.Modules.Partners.Features.DeactivateCarrier;
using ResolveOps.Modules.Partners.Features.GetBusinessCalendar;
using ResolveOps.Modules.Partners.Features.GetCarrier;
using ResolveOps.Modules.Partners.Features.GetCustomer;
using ResolveOps.Modules.Partners.Features.GetLocation;
using ResolveOps.Modules.Partners.Features.ListBusinessCalendars;
using ResolveOps.Modules.Partners.Features.ListCarriers;
using ResolveOps.Modules.Partners.Features.ListCustomers;
using ResolveOps.Modules.Partners.Features.ListLocations;
using ResolveOps.Modules.Partners.Features.RemoveHoliday;
using ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;
using ResolveOps.Modules.Partners.Features.UpdateCarrier;
using ResolveOps.Modules.Partners.Features.UpdateCustomer;
using ResolveOps.Modules.Partners.Features.UpdateLocation;

namespace ResolveOps.Modules.Partners;

public static class PartnersModule
{
    public static IServiceCollection AddPartnersModule(this IServiceCollection services)
    {
        var assembly = typeof(PartnersModule).Assembly;

        // Register Validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-Register Handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-Register Endpoints
        services.AddEndpointsFromAssembly(assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapPartnersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEndpoints();
        return endpoints;
    }
}
