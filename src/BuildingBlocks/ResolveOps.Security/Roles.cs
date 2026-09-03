namespace ResolveOps.Security;

/// <summary>
/// Baseline role names. Values match the claim value stored in JWT and the database.
/// Add new roles here and register them in the Identity seed before use.
/// </summary>
public static class Roles
{
    public const string TenantAdmin = "TenantAdmin";
    public const string OperationsManager = "OperationsManager";
    public const string LogisticsCoordinator = "LogisticsCoordinator";
    public const string ExceptionSpecialist = "ExceptionSpecialist";
    public const string ClaimsSpecialist = "ClaimsSpecialist";
    public const string CustomerService = "CustomerService";
    public const string WarehouseOperator = "WarehouseOperator";
    public const string Finance = "Finance";
    public const string ReadOnlyAuditor = "ReadOnlyAuditor";
    public const string CarrierExternal = "CarrierExternal";

    /// <summary>All defined role names — used for role seeding.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        TenantAdmin,
        OperationsManager,
        LogisticsCoordinator,
        ExceptionSpecialist,
        ClaimsSpecialist,
        CustomerService,
        WarehouseOperator,
        Finance,
        ReadOnlyAuditor,
        CarrierExternal,
    ];
}
