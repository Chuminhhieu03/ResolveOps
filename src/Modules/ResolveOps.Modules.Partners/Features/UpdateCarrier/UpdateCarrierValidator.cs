using FluentValidation;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Features.UpdateCarrier;

public sealed class UpdateCarrierValidator : AbstractValidator<UpdateCarrierCommand>
{
    public UpdateCarrierValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ScacOrExternalCode)
            .MaximumLength(50)
            .When(x => x.ScacOrExternalCode is not null);

        RuleFor(x => x.DefaultTimezone)
            .MaximumLength(100)
            .Must(tz => tz is null || IsValidTimezone(tz))
            .WithMessage("DefaultTimezone must be a valid IANA timezone identifier.")
            .When(x => x.DefaultTimezone is not null);

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .MaximumLength(320)
            .When(x => x.ContactEmail is not null);

        RuleFor(x => x.ClaimSubmissionChannel)
            .NotEmpty()
            .Must(ClaimSubmissionChannel.IsValid)
            .WithMessage($"ClaimSubmissionChannel must be one of: {string.Join(", ", ClaimSubmissionChannel.All)}.");

        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }

    private static bool IsValidTimezone(string id)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}
