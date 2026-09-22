using FluentValidation;

namespace ResolveOps.Modules.Claims.Features.SupplyAdditionalInformation;

public class SupplyAdditionalInformationValidator : AbstractValidator<SupplyAdditionalInformationCommand>
{
    public SupplyAdditionalInformationValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ResponseNotes).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RecordedBy).NotEmpty();
    }
}
