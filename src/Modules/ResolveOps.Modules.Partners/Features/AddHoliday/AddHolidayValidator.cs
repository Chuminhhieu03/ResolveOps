using FluentValidation;

namespace ResolveOps.Modules.Partners.Features.AddHoliday;

public sealed class AddHolidayValidator : AbstractValidator<AddHolidayCommand>
{
    public AddHolidayValidator()
    {
        RuleFor(x => x.CalendarId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.HolidayDate).NotEmpty();
    }
}
