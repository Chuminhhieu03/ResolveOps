using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.AddLossComponent;

public class AddLossComponentValidator : AbstractValidator<AddLossComponentCommand>
{
    public AddLossComponentValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ComponentType).IsEnumName(typeof(LossComponentType));
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
