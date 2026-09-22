using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.GetClaimRecoveries;

public class GetClaimRecoveriesValidator : AbstractValidator<GetClaimRecoveriesQuery>
{
    public GetClaimRecoveriesValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
    }
}
