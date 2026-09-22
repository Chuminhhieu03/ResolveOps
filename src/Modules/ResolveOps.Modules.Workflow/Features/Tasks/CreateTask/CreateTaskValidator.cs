using FluentValidation;
using ResolveOps.Domain.Workflow;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.TaskType)
            .NotEmpty()
            .MaximumLength(50)
            .Must(WorkflowTaskType.IsValid)
            .WithMessage(x => $"Invalid TaskType '{x.TaskType}'. Allowed values: {string.Join(", ", WorkflowTaskType.All)}");
    }
}
