using ResolveOps.Domain.Workflow;

namespace ResolveOps.Modules.Workflow.Models;

public static class WorkflowTaskMappingExtensions
{
    public static WorkflowTaskResponse ToResponse(this WorkflowTask task) =>
        new(
            task.Id,
            task.CaseId,
            task.ClaimId,
            task.TaskType,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.OwnerUserId,
            task.OwnerTeamCode,
            task.DueAtUtc,
            task.BlockedReason,
            task.CompletionNote,
            task.CompletedAtUtc,
            task.IsMandatory,
            task.WaivedReason,
            task.WaivedByUserId,
            task.WaivedAtUtc,
            task.CreatedAtUtc,
            task.ConcurrencyStamp);
}
