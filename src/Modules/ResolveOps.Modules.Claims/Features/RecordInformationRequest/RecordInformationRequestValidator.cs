using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.RecordInformationRequest;

public class RecordInformationRequestValidator : AbstractValidator<RecordInformationRequestCommand>
{
    public RecordInformationRequestValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ResponseAtUtc).NotEmpty();
        RuleFor(x => x.InfoDeadlineAtUtc).NotEmpty();
        RuleFor(x => x.RecordedBy).NotEmpty();
        RuleFor(x => x.SourceChannel).NotEmpty().Must(sc => SourceChannel.All.Contains(sc))
            .WithMessage("Invalid source channel.");
    }
}
