using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.BlockTask;

public sealed class BlockTaskValidator : AbstractValidator<BlockTaskCommand>
{
    public BlockTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
