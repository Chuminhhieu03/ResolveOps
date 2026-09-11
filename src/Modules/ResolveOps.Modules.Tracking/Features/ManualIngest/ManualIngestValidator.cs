using FluentValidation;
using ResolveOps.Domain.Tracking;

namespace ResolveOps.Modules.Tracking.Features.ManualIngest;

public sealed class ManualIngestValidator : AbstractValidator<ManualIngestCommand>
{
    public ManualIngestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CarrierId)
            .NotEmpty()
            .WithMessage("Carrier ID is required.");

        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .MaximumLength(150)
            .WithMessage("Tracking number is required and must not exceed 150 characters.");

        RuleFor(x => x.EventType)
            .NotEmpty()
            .Must(t => TrackingEventType.All.Contains(t))
            .WithMessage($"Event type must be one of: {string.Join(", ", TrackingEventType.All)}");

        RuleFor(x => x.OccurredAtUtc)
            .NotEmpty()
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow().AddMinutes(5))
            .WithMessage("OccurredAtUtc cannot be in the future.");
    }
}
