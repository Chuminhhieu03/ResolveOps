using FluentValidation;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Features.CreateCarrier;

public sealed class CreateCarrierValidator : AbstractValidator<CreateCarrierCommand>
{
    public CreateCarrierValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

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
    }

    // Assumption A-Phase3-001: validate timezone using TimeZoneInfo on the host OS.
    // On Windows this accepts IANA IDs via .NET 6+ mapping. On Linux IANA IDs work natively.
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
