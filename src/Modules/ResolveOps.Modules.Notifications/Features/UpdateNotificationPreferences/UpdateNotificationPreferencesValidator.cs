using FluentValidation;
using ResolveOps.Domain.Notifications;

namespace ResolveOps.Modules.Notifications.Features.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Preferences).NotEmpty();
        RuleForEach(x => x.Preferences).ChildRules(pref =>
        {
            pref.RuleFor(p => p.NotificationClass)
                .NotEmpty()
                .Must(NotificationClass.IsValid)
                .WithMessage(p => $"Invalid notification class '{p.NotificationClass}'.");

            pref.RuleFor(p => p.Channel)
                .NotEmpty()
                .Must(NotificationChannel.IsValid)
                .WithMessage(p => $"Invalid notification channel '{p.Channel}'.");
        });
    }
}
