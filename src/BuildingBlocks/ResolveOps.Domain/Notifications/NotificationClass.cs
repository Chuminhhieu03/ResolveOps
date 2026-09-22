using System;
using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Notifications;

public static class NotificationClass
{
    public const string SlaBreachAlert = "SlaBreachAlert";
    public const string ExceptionEscalation = "ExceptionEscalation";
    public const string TaskAssigned = "TaskAssigned";
    public const string ClaimReviewRequested = "ClaimReviewRequested";
    public const string ClaimDecisionReceived = "ClaimDecisionReceived";
    public const string ClaimRecoveryRecorded = "ClaimRecoveryRecorded";

    public static readonly IReadOnlyList<string> All =
    [
        SlaBreachAlert,
        ExceptionEscalation,
        TaskAssigned,
        ClaimReviewRequested,
        ClaimDecisionReceived,
        ClaimRecoveryRecorded
    ];

    public static readonly IReadOnlySet<string> Mandatory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SlaBreachAlert,
        ExceptionEscalation,
        ClaimReviewRequested
    };

    public static bool IsMandatory(string notificationClass) =>
        !string.IsNullOrWhiteSpace(notificationClass) && Mandatory.Contains(notificationClass);

    public static bool IsValid(string? notificationClass) =>
        !string.IsNullOrWhiteSpace(notificationClass) && All.Contains(notificationClass);
}
