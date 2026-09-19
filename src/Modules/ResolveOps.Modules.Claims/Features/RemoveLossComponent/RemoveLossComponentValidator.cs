using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.RemoveLossComponent;

public class RemoveLossComponentValidator : AbstractValidator<RemoveLossComponentCommand>
{
    public RemoveLossComponentValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
    }
}
