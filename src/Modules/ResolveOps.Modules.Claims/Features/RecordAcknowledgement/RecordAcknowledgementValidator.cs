using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.RecordAcknowledgement;

public class RecordAcknowledgementValidator : AbstractValidator<RecordAcknowledgementCommand>
{
    public RecordAcknowledgementValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ResponseAtUtc).NotEmpty();
        RuleFor(x => x.RecordedBy).NotEmpty();
        RuleFor(x => x.SourceChannel).NotEmpty().Must(sc => SourceChannel.All.Contains(sc))
            .WithMessage("Invalid source channel.");
    }
}
