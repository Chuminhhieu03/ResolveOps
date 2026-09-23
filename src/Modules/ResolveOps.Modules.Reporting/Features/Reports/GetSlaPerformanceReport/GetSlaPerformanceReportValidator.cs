using System;
using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed class GetSlaPerformanceReportValidator : AbstractValidator<GetSlaPerformanceReportQuery>
{
    public GetSlaPerformanceReportValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}
