using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Application.Notifications;
using ResolveOps.Modules.Notifications.Hubs;
using ResolveOps.Modules.Notifications.Services;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(NotificationsModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext
        AppDbContext.AddConfigurationAssembly(assembly);

        // Register validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-register handlers
        services.AddHandlersFromAssembly(assembly);

        // Auto-register endpoints
        services.AddEndpointsFromAssembly(assembly);

        // SMTP Options and Email Sender
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Realtime SignalR Service
        services.AddSignalR();
        services.AddScoped<INotificationRealtimeService, SignalRNotificationRealtimeService>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(NotificationsModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);

        // Map SignalR Hub
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        return endpoints;
    }
}
