using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.GetClaimTimeline;

public class GetClaimTimelineValidator : AbstractValidator<GetClaimTimelineQuery>
{
    public GetClaimTimelineValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
    }
}
