using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.ReturnToDraft;

public class ReturnToDraftValidator : AbstractValidator<ReturnToDraftCommand>
{
    public ReturnToDraftValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
