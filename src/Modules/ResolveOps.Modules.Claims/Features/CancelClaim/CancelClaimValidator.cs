using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.CancelClaim;

public class CancelClaimValidator : AbstractValidator<CancelClaimCommand>
{
    public CancelClaimValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CancelledBy).NotEmpty();
    }
}
