using FluentValidation;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Features.CreateLocation;

public sealed class CreateLocationValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AddressLine1).MaximumLength(250);
        RuleFor(x => x.AddressLine2).MaximumLength(250);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(30);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$").WithMessage("CountryCode must be exactly 2 letters (ISO 3166-1 alpha-2).");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(100)
            .Must(IsValidTimezone)
            .WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue);

        // Lat/Lon must both be provided or neither
        RuleFor(x => x.Latitude)
            .NotNull()
            .When(x => x.Longitude.HasValue)
            .WithMessage("Latitude is required when Longitude is provided.");

        RuleFor(x => x.Longitude)
            .NotNull()
            .When(x => x.Latitude.HasValue)
            .WithMessage("Longitude is required when Latitude is provided.");
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
