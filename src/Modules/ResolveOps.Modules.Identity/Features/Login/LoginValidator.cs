using FluentValidation;

namespace ResolveOps.Modules.Identity.Features.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
