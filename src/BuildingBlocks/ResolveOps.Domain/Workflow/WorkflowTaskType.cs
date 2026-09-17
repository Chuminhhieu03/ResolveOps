namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Common task types in operational exception handling (spec §8.5).
/// </summary>
public static class WorkflowTaskType
{
    public const string DamageInspection = "DamageInspection";
    public const string CarrierLocationUpdate = "CarrierLocationUpdate";
    public const string WarehouseCount = "WarehouseCount";
    public const string CustomerNotification = "CustomerNotification";
    public const string Redelivery = "Redelivery";
    public const string PackagePreservation = "PackagePreservation";
    public const string PodRequest = "PodRequest";
    public const string ManualAction = "ManualAction";
}
