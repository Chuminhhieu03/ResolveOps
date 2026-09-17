using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using ResolveOps.Domain.Identity;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Persistence;

namespace ResolveOps.Api.Infrastructure;

public static class DevelopmentSeeder
{
    private static readonly string[] _damageKeywords =
    [
        "damage", "damaged", "broken", "wet", "crushed", "tampered", "destroyed", "leak", "torn"
    ];

    private static readonly string[] _damageTriggerEventTypes = ["Damaged", "Exception"];

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

        // 3. Seed Standard Parameterized Error Templates
        var errorTemplates = new (string Code, string MessageTemplate, int HttpStatusCode)[]
        {
            ("ERR_SHIPMENT_DUPLICATE_REFERENCE", "A shipment with external reference '{0}' from source '{1}' already exists.", 409),
            ("ERR_SHIPMENT_NOT_FOUND", "Shipment with ID '{0}' was not found.", 404),
            ("ERR_SHIPMENT_CANNOT_CANCEL", "A shipment with status '{0}' cannot be cancelled.", 409),
            ("ERR_CARRIER_NOT_FOUND", "Carrier with ID '{0}' was not found.", 404),
            ("ERR_QUARANTINED_EVENT_NOT_FOUND", "Quarantined event with ID '{0}' was not found.", 404),
            ("ERR_INVALID_STATE_TRANSITION", "Resource '{0}' cannot transition to status '{1}' from current status '{2}'.", 409),
            ("ERR_SHIPMENT_UNMATCHED", "Could not match tracking number '{0}' to any shipment or alias for carrier '{1}'.", 422),
            ("ERR_INVALID_SIGNATURE", "The webhook request HMAC signature is missing or invalid for carrier '{0}'.", 401),
            ("IDEMPOTENCY_KEY_REUSED", "The idempotency key '{0}' was previously used with a different request payload.", 409),
            ("ERR_IDEMPOTENCY_KEY_CONFLICT", "The idempotency key '{0}' was previously used with a different request payload.", 409),
            ("CONCURRENCY_CONFLICT", "The resource '{0}' has been modified by another operation. Expected version/stamp was '{1}'.", 409),
            ("ERR_CONCURRENCY_CONFLICT", "The resource '{0}' has been modified by another operation. Expected version/stamp was '{1}'.", 409),
            ("VALIDATION_FAILED", "Input validation failed for field '{0}': {1}.", 400),
            ("ERR_VALIDATION_FAILED", "Input validation failed for field '{0}': {1}.", 400),
            ("FORBIDDEN", "Action '{0}' is forbidden for current user or tenant '{1}'.", 403),
            ("ERR_FORBIDDEN", "Action '{0}' is forbidden for current user or tenant '{1}'.", 403),
            ("RESOURCE_NOT_FOUND", "The requested resource '{0}' with key '{1}' was not found.", 404),
            ("ERR_RESOURCE_NOT_FOUND", "The requested resource '{0}' with key '{1}' was not found.", 404),
            ("MANDATORY_EVIDENCE_MISSING", "Mandatory evidence '{0}' is required before claim '{1}' can be submitted.", 422),
            ("CLAIM_DEADLINE_EXPIRED", "Claim submission deadline for case '{0}' expired at '{1}'.", 422),
            ("INTEGRATION_TEMPORARILY_UNAVAILABLE", "External carrier integration '{0}' is temporarily unreachable. Next retry at '{1}'.", 503),
            ("ERR_EXCEPTION_POLICY_NOT_FOUND", "Exception policy with key '{0}' was not found.", 404),
            ("ERR_ACTIVE_POLICY_NOT_FOUND", "No active policy found for exception type '{0}'.", 404),
            ("ERR_POLICY_VERSION_EXISTS", "Policy '{0}' with version {1} already exists for this tenant.", 409),
            ("ERR_CASE_CANNOT_CANCEL", "Case '{0}' in status '{1}' cannot be cancelled as a false positive.", 409),
            ("ERR_EXCEPTION_CASE_NOT_FOUND", "Exception case with ID '{0}' was not found.", 404),
        };

        foreach (var (code, template, statusCode) in errorTemplates)
        {
            var existing = context.ErrorTemplates.FirstOrDefault(t => t.Code == code);
            if (existing == null)
            {
                context.ErrorTemplates.Add(ResolveOps.Domain.ErrorTemplate.Create(code, template, statusCode));
            }
            else
            {
                existing.Update(template, statusCode);
            }
        }

        // 4. Seed Default Active Exception Policies for local-dev Tenant (spec §24 Phase 7)
        var existingPolicies = context.ExceptionPolicies.Where(p => p.TenantId == tenant.Id).ToList();
        var now = timeProvider.GetUtcNow();

        if (!existingPolicies.Any(p => p.ExceptionType == ResolveOps.Domain.Exceptions.ExceptionType.PickupDelay))
        {
            var pickupPolicy = ResolveOps.Domain.Exceptions.ExceptionPolicy.Create(
                tenant.Id,
                policyKey: "POL-PICKUP-DELAY-DEFAULT",
                versionNumber: 1,
                exceptionType: ResolveOps.Domain.Exceptions.ExceptionType.PickupDelay,
                ruleDefinitionJson: JsonSerializer.Serialize(new { ToleranceMinutes = 30 }),
                severityDefinitionJson: JsonSerializer.Serialize(new { DefaultSeverity = "Medium", HighValueThreshold = 5000m, CriticalValueThreshold = 25000m }),
                assignmentDefinitionJson: JsonSerializer.Serialize(new { DefaultTeamCode = "OPS-NORTH" }),
                createdByUserId: admin?.Id ?? Guid.Empty,
                effectiveFromUtc: now.AddDays(-30),
                effectiveToUtc: null,
                evidencePolicyVersionId: null,
                slaPolicyVersionId: null,
                status: ResolveOps.Domain.Exceptions.PolicyStatus.Active);

            context.ExceptionPolicies.Add(pickupPolicy);
        }

        if (!existingPolicies.Any(p => p.ExceptionType == ResolveOps.Domain.Exceptions.ExceptionType.InTransitDelay))
        {
            var inTransitPolicy = ResolveOps.Domain.Exceptions.ExceptionPolicy.Create(
                tenant.Id,
                policyKey: "POL-IN-TRANSIT-DELAY-DEFAULT",
                versionNumber: 1,
                exceptionType: ResolveOps.Domain.Exceptions.ExceptionType.InTransitDelay,
                ruleDefinitionJson: JsonSerializer.Serialize(new { ToleranceMinutes = 60, TriggerOnCarrierDelayEvent = true }),
                severityDefinitionJson: JsonSerializer.Serialize(new { DefaultSeverity = "Medium", HighValueThreshold = 5000m, CriticalValueThreshold = 25000m }),
                assignmentDefinitionJson: JsonSerializer.Serialize(new { DefaultTeamCode = "OPS-NORTH" }),
                createdByUserId: admin?.Id ?? Guid.Empty,
                effectiveFromUtc: now.AddDays(-30),
                effectiveToUtc: null,
                evidencePolicyVersionId: null,
                slaPolicyVersionId: null,
                status: ResolveOps.Domain.Exceptions.PolicyStatus.Active);

            context.ExceptionPolicies.Add(inTransitPolicy);
        }

        if (!existingPolicies.Any(p => p.ExceptionType == ResolveOps.Domain.Exceptions.ExceptionType.Damage))
        {
            var damagePolicy = ResolveOps.Domain.Exceptions.ExceptionPolicy.Create(
                tenant.Id,
                policyKey: "POL-DAMAGE-DEFAULT",
                versionNumber: 1,
                exceptionType: ResolveOps.Domain.Exceptions.ExceptionType.Damage,
                ruleDefinitionJson: JsonSerializer.Serialize(new
                {
                    DamageKeywords = _damageKeywords,
                    TriggerEventTypes = _damageTriggerEventTypes
                }),
                severityDefinitionJson: JsonSerializer.Serialize(new { DefaultSeverity = "High", HighValueThreshold = 5000m, CriticalValueThreshold = 25000m }),
                assignmentDefinitionJson: JsonSerializer.Serialize(new { DefaultTeamCode = "CLAIMS-NORTH" }),
                createdByUserId: admin?.Id ?? Guid.Empty,
                effectiveFromUtc: now.AddDays(-30),
                effectiveToUtc: null,
                evidencePolicyVersionId: null,
                slaPolicyVersionId: null,
                status: ResolveOps.Domain.Exceptions.PolicyStatus.Active);

            context.ExceptionPolicies.Add(damagePolicy);
        }

        await context.SaveChangesAsync();
    }
}
