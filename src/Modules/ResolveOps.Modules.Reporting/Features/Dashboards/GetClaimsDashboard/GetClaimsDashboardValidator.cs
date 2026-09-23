using System;
using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetClaimsDashboard;

public sealed class GetClaimsDashboardValidator : AbstractValidator<GetClaimsDashboardQuery>
{
    public GetClaimsDashboardValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");
    }
}
