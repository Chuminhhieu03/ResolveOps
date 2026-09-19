using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.UnblockTask;

public sealed class UnblockTaskValidator : AbstractValidator<UnblockTaskCommand>
{
    public UnblockTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
