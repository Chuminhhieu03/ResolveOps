using FluentValidation;

namespace ResolveOps.Modules.Shipments.Features.CreateShipment;

internal sealed class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(c => c.ExternalReference)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.SourceSystem)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(c => c.CustomerId)
            .NotEmpty();

        RuleFor(c => c.OriginLocationId)
            .NotEmpty();

        RuleFor(c => c.DestinationLocationId)
            .NotEmpty();

        RuleFor(c => c.PlannedPickupAt)
            .NotEmpty();

        RuleFor(c => c.PlannedDeliveryAt)
            .NotEmpty()
            .GreaterThan(c => c.PlannedPickupAt)
            .WithMessage("PlannedDeliveryAt must be after PlannedPickupAt.");

        RuleFor(c => c.ServiceLevel)
            .MaximumLength(50)
            .When(c => c.ServiceLevel is not null);

        // Declared value: if amount present, currency is required and vice versa
        RuleFor(c => c.DeclaredValueCurrency)
            .NotEmpty()
            .MaximumLength(3)
            .MinimumLength(3)
            .When(c => c.DeclaredValue.HasValue)
            .WithMessage("DeclaredValueCurrency is required when DeclaredValue is provided.");

        RuleFor(c => c.DeclaredValue)
            .GreaterThan(0)
            .When(c => c.DeclaredValue.HasValue);

        RuleFor(c => c.ExpectedPackageCount)
            .GreaterThan(0)
            .When(c => c.ExpectedPackageCount.HasValue);

        RuleFor(c => c.ExpectedWeight)
            .GreaterThan(0)
            .When(c => c.ExpectedWeight.HasValue);

        RuleFor(c => c.Legs)
            .NotNull();

        RuleForEach(c => c.Legs).ChildRules(leg =>
        {
            leg.RuleFor(l => l.SequenceNumber).GreaterThan(0);
            leg.RuleFor(l => l.CarrierId).NotEmpty();
            leg.RuleFor(l => l.OriginLocationId).NotEmpty();
            leg.RuleFor(l => l.DestinationLocationId).NotEmpty();
            leg.RuleFor(l => l.TrackingNumber)
                .MaximumLength(100)
                .When(l => l.TrackingNumber is not null);
        });

        RuleFor(c => c.Items)
            .NotNull();

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.LineReference).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.ExpectedQuantity).GreaterThan(0);
            item.RuleFor(i => i.QuantityUnit).NotEmpty().MaximumLength(20);
        });
    }
}
