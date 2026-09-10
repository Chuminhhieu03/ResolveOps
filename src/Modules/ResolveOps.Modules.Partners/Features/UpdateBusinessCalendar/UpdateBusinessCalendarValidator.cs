using FluentValidation;
using ResolveOps.Domain.Partners;

namespace ResolveOps.Modules.Partners.Features.UpdateBusinessCalendar;

public sealed class UpdateBusinessCalendarValidator : AbstractValidator<UpdateBusinessCalendarCommand>
{
    public UpdateBusinessCalendarValidator()
    {
        RuleFor(x => x.CalendarId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(100)
            .Must(IsValidTimezone)
            .WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.WorkingDaysMask)
            .InclusiveBetween(1, WorkingDaysMasks.AllDays)
            .WithMessage("WorkingDaysMask must be between 1 and 127.");

        RuleFor(x => x.WorkingStart)
            .LessThan(x => x.WorkingEnd)
            .WithMessage("WorkingStart must be earlier than WorkingEnd.");

        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
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
