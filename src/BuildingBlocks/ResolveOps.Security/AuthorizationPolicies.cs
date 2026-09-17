using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ResolveOps.Security;

/// <summary>
/// Policy names used in <see cref="AuthorizeAttribute"/> and
/// <see cref="Microsoft.AspNetCore.Builder.AuthorizationEndpointConventionBuilderExtensions.RequireAuthorization"/>.
///
/// Each policy name maps to a required permission claim.
/// The naming convention is: require_{permission_name} (lowercase).
/// </summary>
public static class AuthorizationPolicies
{
    public const string RequireActiveTenantMembership = "require_active_tenant_membership";
    public const string RequireManageTenant = "require_manage_tenant";
    public const string RequireManageUsers = "require_manage_users";
    public const string RequireViewAudit = "require_view_audit";
    public const string RequireManagePolicies = "require_manage_policies";
    public const string RequireManageIntegrations = "require_manage_integrations";

    public const string RequireViewCases = "require_view_cases";
    public const string RequireCreateExceptionCase = "require_create_exception_case";
    public const string RequireTriageCase = "require_triage_case";
    public const string RequireAssignCase = "require_assign_case";
    public const string RequireUpdateCase = "require_update_case";
    public const string RequireCloseCase = "require_close_case";
    public const string RequireCancelCase = "require_cancel_case";
    public const string RequireReopenCase = "require_reopen_closed_case";

    public const string RequireViewClaims = "require_view_claims";
    public const string RequireCreateClaim = "require_create_claim";
    public const string RequireApproveClaimSubmission = "require_approve_claim_submission";
    public const string RequireRecordCarrierDecision = "require_record_carrier_decision";
    public const string RequireRecordRecovery = "require_record_recovery";

    public const string RequireUploadEvidence = "require_upload_evidence";
    public const string RequireDownloadEvidence = "require_download_evidence";

    public const string RequireCreateShipment = "require_create_shipment";
    public const string RequireViewShipments = "require_view_shipments";
    public const string RequireCancelShipment = "require_cancel_shipment";

    public const string RequireViewCarriers = "require_view_carriers";
    public const string RequireManageCarriers = "require_manage_carriers";
    public const string RequireViewCustomers = "require_view_customers";
    public const string RequireManageCustomers = "require_manage_customers";
    public const string RequireViewLocations = "require_view_locations";
    public const string RequireManageLocations = "require_manage_locations";
    public const string RequireManageBusinessCalendars = "require_manage_business_calendars";

    /// <summary>
    /// Registers all permission-based authorization policies.
    /// Call from Program.cs or module DI registration.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(RequireManageTenant, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageTenant))
            .AddPolicy(RequireManageUsers, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageUsers))
            .AddPolicy(RequireViewAudit, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewAudit))
            .AddPolicy(RequireManagePolicies, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManagePolicies))
            .AddPolicy(RequireManageIntegrations, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageIntegrations))
            .AddPolicy(RequireViewCases, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewCases))
            .AddPolicy(RequireCreateExceptionCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCreateExceptionCase))
            .AddPolicy(RequireTriageCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanTriageCase))
            .AddPolicy(RequireAssignCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanAssignCase))
            .AddPolicy(RequireUpdateCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanUpdateCase))
            .AddPolicy(RequireCloseCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCloseCase))
            .AddPolicy(RequireCancelCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCancelCase))
            .AddPolicy(RequireReopenCase, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanReopenClosedCase))
            .AddPolicy(RequireViewClaims, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewClaims))
            .AddPolicy(RequireCreateClaim, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCreateClaim))
            .AddPolicy(RequireApproveClaimSubmission, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanApproveClaimSubmission))
            .AddPolicy(RequireRecordCarrierDecision, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanRecordCarrierDecision))
            .AddPolicy(RequireRecordRecovery, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanRecordRecovery))
            .AddPolicy(RequireUploadEvidence, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanUploadEvidence))
            .AddPolicy(RequireDownloadEvidence, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanDownloadEvidence))
            .AddPolicy(RequireCreateShipment, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCreateShipment))
            .AddPolicy(RequireViewShipments, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewShipments))
            .AddPolicy(RequireCancelShipment, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanCancelShipment))
            .AddPolicy(RequireViewCarriers, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewCarriers))
            .AddPolicy(RequireManageCarriers, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageCarriers))
            .AddPolicy(RequireViewCustomers, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewCustomers))
            .AddPolicy(RequireManageCustomers, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageCustomers))
            .AddPolicy(RequireViewLocations, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanViewLocations))
            .AddPolicy(RequireManageLocations, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageLocations))
            .AddPolicy(RequireManageBusinessCalendars, p => p.RequireClaim(ClaimTypes.Permission, Permissions.CanManageBusinessCalendars));

        return services;
    }

    /// <summary>JWT claim type for permission strings.</summary>
    public static class ClaimTypes
    {
        public const string Permission = "permission";
        public const string TenantId = "tenant_id";
        public const string MembershipId = "membership_id";
    }
}
