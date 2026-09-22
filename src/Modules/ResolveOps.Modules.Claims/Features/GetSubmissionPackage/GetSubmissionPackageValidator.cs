using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.GetSubmissionPackage;

public class GetSubmissionPackageValidator : AbstractValidator<GetSubmissionPackageQuery>
{
    public GetSubmissionPackageValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
    }
}
