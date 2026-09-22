using FluentValidation;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.RecordRecovery;

public class RecordRecoveryValidator : AbstractValidator<RecordRecoveryCommand>
{
    public RecordRecoveryValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.TransactionType)
            .NotEmpty()
            .Must(t => RecoveryTransactionType.IsValid(t))
            .WithMessage("Invalid transaction type. Must be Payment, CreditNote, or Adjustment.");
        RuleFor(x => x.ExternalReference)
            .NotEmpty()
            .MaximumLength(150);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);
        RuleFor(x => x.ReceivedAtUtc).NotEmpty();
        RuleFor(x => x.RecordedBy).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);

        When(x => x.TransactionType == RecoveryTransactionType.Payment || x.TransactionType == RecoveryTransactionType.CreditNote, () =>
        {
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment or CreditNote amount must be greater than zero.");
        });

        When(x => x.TransactionType == RecoveryTransactionType.Adjustment, () =>
        {
            RuleFor(x => x.Amount).NotEqual(0).WithMessage("Adjustment amount cannot be zero.");
        });
    }
}
