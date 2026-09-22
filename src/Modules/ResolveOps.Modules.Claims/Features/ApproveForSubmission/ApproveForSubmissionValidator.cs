using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.ApproveForSubmission;

public class ApproveForSubmissionValidator : AbstractValidator<ApproveForSubmissionCommand>
{
    public ApproveForSubmissionValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ApproverId).NotEmpty();
    }
}
