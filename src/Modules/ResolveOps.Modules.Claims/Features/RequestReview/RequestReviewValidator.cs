using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.RequestReview;

public class RequestReviewValidator : AbstractValidator<RequestReviewCommand>
{
    public RequestReviewValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
