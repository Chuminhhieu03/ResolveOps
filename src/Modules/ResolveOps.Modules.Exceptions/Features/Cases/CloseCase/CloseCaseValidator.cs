using FluentValidation;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CloseCase;

public sealed class CloseCaseValidator : AbstractValidator<CloseCaseCommand>
{
    public CloseCaseValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
