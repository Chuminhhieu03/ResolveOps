using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.CloseClaim;

public class CloseClaimValidator : AbstractValidator<CloseClaimCommand>
{
    public CloseClaimValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ClosedBy).NotEmpty();
        RuleFor(x => x.ClosingNotes).MaximumLength(1000);
    }
}
