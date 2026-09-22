using System;
using System.Collections.Generic;

namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Common task types in operational exception handling and claims workflow (spec §8.5, §9.3).
/// </summary>
public static class WorkflowTaskType
{
    // ── Exception & Transit Operations ──────────────────────────────────
    public const string DamageInspection = "DamageInspection";
    public const string CarrierLocationUpdate = "CarrierLocationUpdate";
    public const string WarehouseCount = "WarehouseCount";
    public const string Redelivery = "Redelivery";
    public const string PackagePreservation = "PackagePreservation";
    public const string PodRequest = "PodRequest";

    // ── Claims Operations (Phase 11+) ───────────────────────────────────
    public const string ClaimFollowUp = "ClaimFollowUp";
    public const string SupplyInformation = "SupplyInformation";

    // ── Communication & General ─────────────────────────────────────────
    public const string CustomerNotification = "CustomerNotification";
    public const string ManualAction = "ManualAction";
    public const string Custom = "Custom";

    public static readonly IReadOnlyList<string> All =
    [
        DamageInspection,
        CarrierLocationUpdate,
        WarehouseCount,
        Redelivery,
        PackagePreservation,
        PodRequest,
        ClaimFollowUp,
        SupplyInformation,
        CustomerNotification,
        ManualAction,
        Custom
    ];

    private static readonly HashSet<string> _allSet = new(All, StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? taskType) =>
        !string.IsNullOrWhiteSpace(taskType) && _allSet.Contains(taskType.Trim());
}
