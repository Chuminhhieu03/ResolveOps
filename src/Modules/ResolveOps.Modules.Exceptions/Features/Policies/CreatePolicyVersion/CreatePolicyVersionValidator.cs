using FluentValidation;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Modules.Exceptions.Features.Policies.CreatePolicyVersion;

public sealed class CreatePolicyVersionValidator : AbstractValidator<CreatePolicyVersionCommand>
{
    public CreatePolicyVersionValidator()
    {
        RuleFor(x => x.PolicyKey)
            .NotEmpty().WithMessage("Policy key is required.")
            .MaximumLength(100).WithMessage("Policy key must not exceed 100 characters.");

        RuleFor(x => x.VersionNumber)
            .GreaterThan(0).WithMessage("Version number must be greater than zero.");

        RuleFor(x => x.ExceptionType)
            .NotEmpty().WithMessage("Exception type is required.")
            .Must(ExceptionType.IsValid).WithMessage("Exception type must be a valid recognized type.");

        RuleFor(x => x.RuleDefinitionJson)
            .NotEmpty().WithMessage("Rule definition JSON is required.");

        RuleFor(x => x.SeverityDefinitionJson)
            .NotEmpty().WithMessage("Severity definition JSON is required.");

        RuleFor(x => x.AssignmentDefinitionJson)
            .NotEmpty().WithMessage("Assignment definition JSON is required.");
    }
}
