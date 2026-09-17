namespace ResolveOps.Security;

/// <summary>
/// Granular permission strings used by policy-based authorization (spec §19.3).
/// Permissions are checked against claims stored in the JWT "permissions" claim array.
///
/// Naming convention: Can{Verb}{Noun}
/// </summary>
public static class Permissions
{
    // ── Tenant administration ────────────────────────────────────────────────
    public const string CanManageTenant = "CanManageTenant";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanViewAudit = "CanViewAudit";
    public const string CanManagePolicies = "CanManagePolicies";
    public const string CanManageIntegrations = "CanManageIntegrations";

    // ── Operational ──────────────────────────────────────────────────────────
    public const string CanViewCases = "CanViewCases";
    public const string CanCreateExceptionCase = "CanCreateExceptionCase";
    public const string CanTriageCase = "CanTriageCase";
    public const string CanAssignCase = "CanAssignCase";
    public const string CanUpdateCase = "CanUpdateCase";
    public const string CanCloseCase = "CanCloseCase";
    public const string CanCancelCase = "CanCancelCase";
    public const string CanReopenClosedCase = "CanReopenClosedCase";
    public const string CanOverrideSeverity = "CanOverrideSeverity";

    // ── Claims ───────────────────────────────────────────────────────────────
    public const string CanViewClaims = "CanViewClaims";
    public const string CanCreateClaim = "CanCreateClaim";
    public const string CanApproveClaimSubmission = "CanApproveClaimSubmission";
    public const string CanRecordCarrierDecision = "CanRecordCarrierDecision";
    public const string CanRecordRecovery = "CanRecordRecovery";
    public const string CanWriteOffClaim = "CanWriteOffClaim";

    // ── Evidence ─────────────────────────────────────────────────────────────
    public const string CanUploadEvidence = "CanUploadEvidence";
    public const string CanDownloadEvidence = "CanDownloadEvidence";

    // ── Shipments ─────────────────────────────────────────────────────────────
    public const string CanCreateShipment = "CanCreateShipment";
    public const string CanViewShipments = "CanViewShipments";
    public const string CanCancelShipment = "CanCancelShipment";

    // ── Partners ─────────────────────────────────────────────────────────────
    public const string CanViewCarriers = "CanViewCarriers";
    public const string CanManageCarriers = "CanManageCarriers";
    public const string CanViewCustomers = "CanViewCustomers";
    public const string CanManageCustomers = "CanManageCustomers";
    public const string CanViewLocations = "CanViewLocations";
    public const string CanManageLocations = "CanManageLocations";
    public const string CanManageBusinessCalendars = "CanManageBusinessCalendars";

    /// Maps each role to its set of granted permissions.
    /// Permissions are encoded as JWT claims at login time.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Roles.TenantAdmin] =
            [
                CanManageTenant, CanManageUsers, CanViewAudit, CanManagePolicies, CanManageIntegrations,
                CanViewCases, CanCreateExceptionCase, CanTriageCase, CanAssignCase, CanUpdateCase, CanCloseCase, CanCancelCase,
                CanReopenClosedCase, CanOverrideSeverity,
                CanViewClaims, CanCreateClaim, CanApproveClaimSubmission, CanRecordCarrierDecision,
                CanRecordRecovery, CanWriteOffClaim,
                CanUploadEvidence, CanDownloadEvidence,
                CanCreateShipment, CanViewShipments, CanCancelShipment,
                CanViewCarriers, CanManageCarriers,
                CanViewCustomers, CanManageCustomers,
                CanViewLocations, CanManageLocations,
                CanManageBusinessCalendars,
            ],

            [Roles.OperationsManager] =
            [
                CanViewCases, CanCreateExceptionCase, CanTriageCase, CanAssignCase, CanUpdateCase, CanCloseCase, CanCancelCase,
                CanReopenClosedCase, CanOverrideSeverity,
                CanViewClaims, CanCreateClaim,
                CanUploadEvidence, CanDownloadEvidence,
                CanViewShipments,
                CanViewCarriers, CanManageCarriers,
                CanViewCustomers, CanManageCustomers,
                CanViewLocations, CanManageLocations,
                CanManageBusinessCalendars,
            ],

            [Roles.LogisticsCoordinator] =
            [
                CanViewCases, CanCreateExceptionCase, CanTriageCase, CanUpdateCase,
                CanViewClaims,
                CanUploadEvidence, CanDownloadEvidence,
                CanViewShipments,
                CanViewCarriers, CanViewCustomers, CanViewLocations,
            ],

            [Roles.ExceptionSpecialist] =
            [
                CanViewCases, CanCreateExceptionCase, CanTriageCase, CanAssignCase, CanUpdateCase, CanCloseCase, CanCancelCase,
                CanOverrideSeverity,
                CanViewClaims, CanCreateClaim,
                CanUploadEvidence, CanDownloadEvidence,
                CanViewShipments,
                CanViewCarriers, CanViewCustomers, CanViewLocations,
            ],

            [Roles.ClaimsSpecialist] =
            [
                CanViewCases,
                CanViewClaims, CanCreateClaim, CanApproveClaimSubmission, CanRecordCarrierDecision,
                CanRecordRecovery,
                CanUploadEvidence, CanDownloadEvidence,
                CanViewShipments,
                CanViewCarriers, CanViewCustomers, CanViewLocations,
            ],

            [Roles.CustomerService] =
            [
                CanViewCases,
                CanViewClaims,
                CanDownloadEvidence,
                CanViewShipments,
            ],
            [Roles.WarehouseOperator] =
            [
                CanViewCases,
                CanUploadEvidence, CanDownloadEvidence,
                CanViewShipments,
            ],
            [Roles.Finance] =
            [
                CanViewClaims, CanRecordRecovery, CanWriteOffClaim,
                CanDownloadEvidence,
                CanViewShipments,
            ],
            [Roles.ReadOnlyAuditor] =
            [
                CanViewCases, CanViewClaims, CanViewAudit, CanDownloadEvidence, CanViewShipments,
            ],
            [Roles.CarrierExternal] =
            [
                CanDownloadEvidence,
            ],
        };

    /// <summary>Returns all permissions granted by the given set of roles.</summary>
    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            if (RolePermissions.TryGetValue(role, out var perms))
            {
                foreach (var p in perms)
                {
                    result.Add(p);
                }
            }
        }

        return [.. result];
    }
}
