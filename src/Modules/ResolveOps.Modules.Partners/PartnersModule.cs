using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Register Validators
        services.AddValidatorsFromAssembly(typeof(PartnersModule).Assembly, includeInternalTypes: true);

        // Register Handlers
        services.AddScoped<CreateCarrierHandler>();
        services.AddScoped<GetCarrierHandler>();
        services.AddScoped<ListCarriersHandler>();
        services.AddScoped<UpdateCarrierHandler>();
        services.AddScoped<ActivateCarrierHandler>();
        services.AddScoped<DeactivateCarrierHandler>();

        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<GetCustomerHandler>();
        services.AddScoped<ListCustomersHandler>();
        services.AddScoped<UpdateCustomerHandler>();

        services.AddScoped<CreateLocationHandler>();
        services.AddScoped<GetLocationHandler>();
        services.AddScoped<ListLocationsHandler>();
        services.AddScoped<UpdateLocationHandler>();

        services.AddScoped<CreateBusinessCalendarHandler>();
        services.AddScoped<GetBusinessCalendarHandler>();
        services.AddScoped<ListBusinessCalendarsHandler>();
        services.AddScoped<UpdateBusinessCalendarHandler>();
        services.AddScoped<AddHolidayHandler>();
        services.AddScoped<RemoveHolidayHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapPartnersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        CreateCarrierEndpoint.MapEndpoint(endpoints);
        GetCarrierEndpoint.MapEndpoint(endpoints);
        ListCarriersEndpoint.MapEndpoint(endpoints);
        UpdateCarrierEndpoint.MapEndpoint(endpoints);
        ActivateCarrierEndpoint.MapEndpoint(endpoints);
        DeactivateCarrierEndpoint.MapEndpoint(endpoints);

        CreateCustomerEndpoint.MapEndpoint(endpoints);
        GetCustomerEndpoint.MapEndpoint(endpoints);
        ListCustomersEndpoint.MapEndpoint(endpoints);
        UpdateCustomerEndpoint.MapEndpoint(endpoints);

        CreateLocationEndpoint.MapEndpoint(endpoints);
        GetLocationEndpoint.MapEndpoint(endpoints);
        ListLocationsEndpoint.MapEndpoint(endpoints);
        UpdateLocationEndpoint.MapEndpoint(endpoints);

        CreateBusinessCalendarEndpoint.MapEndpoint(endpoints);
        GetBusinessCalendarEndpoint.MapEndpoint(endpoints);
        ListBusinessCalendarsEndpoint.MapEndpoint(endpoints);
        UpdateBusinessCalendarEndpoint.MapEndpoint(endpoints);
        AddHolidayEndpoint.MapEndpoint(endpoints);
        RemoveHolidayEndpoint.MapEndpoint(endpoints);

        return endpoints;
    }
}
