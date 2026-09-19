using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.UpdateLossComponent;

public class UpdateLossComponentValidator : AbstractValidator<UpdateLossComponentCommand>
{
    public UpdateLossComponentValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
