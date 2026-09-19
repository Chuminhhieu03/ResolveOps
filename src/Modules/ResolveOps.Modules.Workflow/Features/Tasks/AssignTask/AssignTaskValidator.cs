using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.AssignTask;

public sealed class AssignTaskValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
