using FluentValidation;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.TaskType).NotEmpty().MaximumLength(50);
    }
}
