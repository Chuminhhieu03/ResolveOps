using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.StartTask;

public sealed class StartTaskValidator : AbstractValidator<StartTaskCommand>
{
    public StartTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
