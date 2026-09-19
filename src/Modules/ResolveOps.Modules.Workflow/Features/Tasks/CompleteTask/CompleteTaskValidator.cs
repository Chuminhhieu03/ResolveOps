using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CompleteTask;

public sealed class CompleteTaskValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
