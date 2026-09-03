using FluentValidation;

namespace ResolveOps.Modules.Tenancy.Features.InviteUser;

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be valid.");

        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles must be provided.");
    }
}
