using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CancelTask;

public sealed class CancelTaskValidator : AbstractValidator<CancelTaskCommand>
{
    public CancelTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
