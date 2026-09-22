using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.RecordDecision;

public class RecordDecisionValidator : AbstractValidator<RecordDecisionCommand>
{
    public RecordDecisionValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Decision).NotEmpty().Must(d => CarrierResponseType.All.Contains(d))
            .WithMessage("Invalid decision type.");
        RuleFor(x => x.ResponseAtUtc).NotEmpty();
        RuleFor(x => x.RecordedBy).NotEmpty();
        RuleFor(x => x.SourceChannel).NotEmpty().Must(sc => SourceChannel.All.Contains(sc))
            .WithMessage("Invalid source channel.");
    }
}
