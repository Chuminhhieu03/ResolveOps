using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using ResolveOps.Domain.Identity;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Persistence;

namespace ResolveOps.Api.Infrastructure;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Tenant
        var defaultTenantCode = "local-dev";
        var tenant = context.Tenants.FirstOrDefault(t => t.Code == defaultTenantCode);
        if (tenant == null)
        {
            tenant = Tenant.Create(defaultTenantCode, "Local Development", "UTC", "USD", timeProvider);
            context.Tenants.Add(tenant);

            var defaultSettingsJson = JsonSerializer.Serialize(new { AllowGuestAccess = false, Theme = "light" });
            var settings = TenantSettings.CreateDefault(tenant.Id, timeProvider);
            settings.Update(defaultSettingsJson, timeProvider);
            context.TenantSettings.Add(settings);

            await context.SaveChangesAsync();
        }

        // 2. Seed Admin User
        var adminEmail = "admin@resolveops.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = ApplicationUser.Create(adminEmail, adminEmail, timeProvider);
            var result = await userManager.CreateAsync(admin, "DevPassword123!");
            if (result.Succeeded)
            {
                // Add membership to local-dev tenant
                var membership = UserTenantMembership.Create(admin.Id, tenant.Id, ["Admin"], timeProvider);
                context.UserTenantMemberships.Add(membership);
                await context.SaveChangesAsync();
            }
        }
    }
}
