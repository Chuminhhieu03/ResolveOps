using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.WaiveTask;

public sealed class WaiveTaskValidator : AbstractValidator<WaiveTaskCommand>
{
    public WaiveTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
