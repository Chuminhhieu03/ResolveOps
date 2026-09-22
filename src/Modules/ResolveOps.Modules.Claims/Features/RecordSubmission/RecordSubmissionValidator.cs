using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public class RecordSubmissionValidator : AbstractValidator<RecordSubmissionCommand>
{
    public RecordSubmissionValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubmittedAtUtc).NotEmpty();
    }
}
