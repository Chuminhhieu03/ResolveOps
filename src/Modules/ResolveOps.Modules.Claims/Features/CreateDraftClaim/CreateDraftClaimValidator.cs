using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.CreateDraftClaim;

public class CreateDraftClaimValidator : AbstractValidator<CreateDraftClaimCommand>
{
    public CreateDraftClaimValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.ClaimType).IsEnumName(typeof(ClaimType));
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
