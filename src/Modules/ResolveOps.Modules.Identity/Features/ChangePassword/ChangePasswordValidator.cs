using FluentValidation;

namespace ResolveOps.Modules.Identity.Features.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password cannot be the same as the current password.");
    }
}
