using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public class WriteOffClaimValidator : AbstractValidator<WriteOffClaimCommand>
{
    public WriteOffClaimValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.WriteOffAmount).GreaterThan(0).WithMessage("Write-off amount must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().Must(r => WriteOffReasonCodes.IsValid(r))
            .WithMessage("Invalid write-off reason code.");
        RuleFor(x => x.ApprovedBy).NotEmpty();
    }
}
