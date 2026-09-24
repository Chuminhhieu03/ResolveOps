using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestCarrierScorecardsExport;

public sealed class RequestCarrierScorecardsExportValidator : AbstractValidator<RequestCarrierScorecardsExportCommand>
{
    public RequestCarrierScorecardsExportValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate!.Value)
                .LessThanOrEqualTo(x => x.ToDate!.Value)
                .WithMessage("FromDate must not be later than ToDate.");
        });
    }
}
