using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResolveOps.Observability;

/// <summary>
/// OpenTelemetry metrics and activity source for workflow tasks, SLA clocks, and case transitions (spec §20.3, §20.4, §24 Phase 8).
/// </summary>
public static class WorkflowMetrics
{
    public const string MeterName = "ResolveOps.Workflow";
    public const string ActivitySourceName = "ResolveOps.Workflow";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    // Counters
    public static readonly Counter<long> TasksCompletedTotal = Meter.CreateCounter<long>(
        "workflow.tasks.completed",
        unit: "{task}",
        description: "Total number of workflow tasks completed");

    public static readonly Counter<long> SlaBreachedTotal = Meter.CreateCounter<long>(
        "workflow.sla.breached",
        unit: "{clock}",
        description: "Total number of SLA clocks breached");

    public static readonly Counter<long> CaseTransitionsTotal = Meter.CreateCounter<long>(
        "workflow.case.transitions",
        unit: "{transition}",
        description: "Total number of exception case state transitions");
}
