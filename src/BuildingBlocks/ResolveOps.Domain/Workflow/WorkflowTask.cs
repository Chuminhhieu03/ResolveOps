namespace ResolveOps.Domain.Workflow;

/// <summary>
/// Workflow task aggregate root (spec §9.3, §15.8).
///
/// Invariants:
/// - State machine:
///   Open -> InProgress -> Completed
///   Open -> Cancelled
///   InProgress -> Blocked -> InProgress
///   Blocked -> Cancelled
/// - Completion requires task to be InProgress.
/// - Mandatory tasks can be waived by an authorized actor with a reason.
/// - Optimistic concurrency via ConcurrencyStamp (ADR-006).
/// </summary>
public sealed class WorkflowTask : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid? ClaimId { get; private set; }
    public string TaskType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Status { get; private set; } = WorkflowTaskStatus.Open;
    public string Priority { get; private set; } = WorkflowTaskPriority.Normal;

    public Guid? OwnerUserId { get; private set; }
    public string? OwnerTeamCode { get; private set; }

    public DateTimeOffset? DueAtUtc { get; private set; }
    public string? BlockedReason { get; private set; }
    public string? CompletionNote { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool IsMandatory { get; private set; }
    public string? WaivedReason { get; private set; }
    public Guid? WaivedByUserId { get; private set; }
    public DateTimeOffset? WaivedAtUtc { get; private set; }

    public Guid? CreatedByPolicyId { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp (ADR-006)
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private WorkflowTask() { }

    public static WorkflowTask Create(
        Guid tenantId,
        Guid caseId,
        string taskType,
        string title,
        string? description,
        string priority,
        Guid? ownerUserId,
        string? ownerTeamCode,
        DateTimeOffset? dueAtUtc,
        bool isMandatory,
        Guid? createdByPolicyId,
        TimeProvider timeProvider,
        Guid? claimId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        var now = timeProvider.GetUtcNow();

        return new WorkflowTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            ClaimId = claimId,
            TaskType = string.IsNullOrWhiteSpace(taskType) ? WorkflowTaskType.ManualAction : taskType,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = WorkflowTaskStatus.Open,
            Priority = string.IsNullOrWhiteSpace(priority) ? WorkflowTaskPriority.Normal : priority,
            OwnerUserId = ownerUserId,
            OwnerTeamCode = ownerTeamCode,
            DueAtUtc = dueAtUtc,
            IsMandatory = isMandatory,
            CreatedByPolicyId = createdByPolicyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public Result Assign(Guid? ownerUserId, string? ownerTeamCode)
    {
        if (WorkflowTaskStatus.IsTerminal(Status))
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Cannot assign task '{Id}' because it is in terminal status '{Status}'.");
        }

        OwnerUserId = ownerUserId;
        OwnerTeamCode = ownerTeamCode;
        return Result.Success();
    }

    public Result Start()
    {
        if (Status != WorkflowTaskStatus.Open)
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Task '{Id}' cannot be started from status '{Status}'. Only 'Open' tasks can be started.");
        }

        Status = WorkflowTaskStatus.InProgress;

        return Result.Success();
    }

    public Result Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "A reason is required to block a task.");
        }

        if (Status != WorkflowTaskStatus.InProgress)
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Task '{Id}' cannot be blocked from status '{Status}'. Only 'InProgress' tasks can be blocked.");
        }

        Status = WorkflowTaskStatus.Blocked;
        BlockedReason = reason.Trim();

        return Result.Success();
    }

    public Result Unblock()
    {
        if (Status != WorkflowTaskStatus.Blocked)
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Task '{Id}' cannot be unblocked from status '{Status}'. Only 'Blocked' tasks can be unblocked.");
        }

        Status = WorkflowTaskStatus.InProgress;
        BlockedReason = null;

        return Result.Success();
    }

    public Result Complete(string? completionNote, TimeProvider timeProvider)
    {
        if (Status != WorkflowTaskStatus.InProgress)
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Task '{Id}' cannot be completed from status '{Status}'. Only 'InProgress' tasks can be completed.");
        }

        Status = WorkflowTaskStatus.Completed;
        CompletionNote = completionNote?.Trim();
        CompletedAtUtc = timeProvider.GetUtcNow();

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != WorkflowTaskStatus.Open && Status != WorkflowTaskStatus.Blocked)
        {
            return DomainError.Failure(
                "ERR_TASK_CANNOT_TRANSITION",
                $"Task '{Id}' cannot be cancelled from status '{Status}'. Only 'Open' or 'Blocked' tasks can be cancelled.");
        }

        Status = WorkflowTaskStatus.Cancelled;

        return Result.Success();
    }

    public Result Waive(Guid waivedByUserId, string reason, TimeProvider timeProvider)
    {
        if (!IsMandatory)
        {
            return DomainError.Failure("ERR_TASK_NOT_MANDATORY", $"Task '{Id}' is not marked as mandatory.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "A waiver reason is required to waive a mandatory task.");
        }

        WaivedByUserId = waivedByUserId;
        WaivedReason = reason.Trim();
        WaivedAtUtc = timeProvider.GetUtcNow();

        return Result.Success();
    }
}
