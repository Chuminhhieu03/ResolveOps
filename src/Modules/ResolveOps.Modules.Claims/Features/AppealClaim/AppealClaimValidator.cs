using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.AppealClaim;

public class AppealClaimValidator : AbstractValidator<AppealClaimCommand>
{
    public AppealClaimValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.AppealReason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AppealedBy).NotEmpty();
    }
}
