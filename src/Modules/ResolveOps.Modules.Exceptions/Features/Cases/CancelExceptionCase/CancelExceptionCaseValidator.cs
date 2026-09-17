using FluentValidation;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CancelExceptionCase;

public sealed class CancelExceptionCaseValidator : AbstractValidator<CancelExceptionCaseCommand>
{
    public CancelExceptionCaseValidator()
    {
        RuleFor(x => x.CaseId)
            .NotEmpty().WithMessage("CaseId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(500).WithMessage("Cancellation reason must not exceed 500 characters.");

        RuleFor(x => x.ConcurrencyStamp)
            .NotEmpty().WithMessage("Concurrency stamp is required for optimistic concurrency.");
    }
}
