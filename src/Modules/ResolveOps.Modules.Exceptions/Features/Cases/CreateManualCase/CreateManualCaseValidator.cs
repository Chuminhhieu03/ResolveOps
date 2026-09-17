using FluentValidation;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CreateManualCase;

public sealed class CreateManualCaseValidator : AbstractValidator<CreateManualCaseCommand>
{
    public CreateManualCaseValidator()
    {
        RuleFor(x => x.ShipmentId)
            .NotEmpty().WithMessage("ShipmentId is required.");

        RuleFor(x => x.ExceptionType)
            .NotEmpty().WithMessage("ExceptionType is required.")
            .Must(ExceptionType.IsValid).WithMessage("ExceptionType must be a valid recognized type.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for manual case creation.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.Severity)
            .Must(s => string.IsNullOrEmpty(s) || ExceptionSeverity.IsValid(s))
            .WithMessage("Severity must be Low, Medium, High, or Critical.");

        RuleFor(x => x.FinancialExposure)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FinancialExposure.HasValue)
            .WithMessage("Financial exposure cannot be negative.");
    }
}
