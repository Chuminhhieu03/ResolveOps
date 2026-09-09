using FluentValidation;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Features.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(CustomerPriority.IsValid)
            .WithMessage($"Priority must be one of: {string.Join(", ", CustomerPriority.All)}.");

        RuleFor(x => x.DefaultTimezone)
            .MaximumLength(100)
            .Must(tz => tz is null || IsValidTimezone(tz))
            .WithMessage("DefaultTimezone must be a valid IANA timezone identifier.")
            .When(x => x.DefaultTimezone is not null);
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
