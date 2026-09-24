using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestClaimsExport;

public sealed class RequestClaimsExportValidator : AbstractValidator<RequestClaimsExportCommand>
{
    public RequestClaimsExportValidator()
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
