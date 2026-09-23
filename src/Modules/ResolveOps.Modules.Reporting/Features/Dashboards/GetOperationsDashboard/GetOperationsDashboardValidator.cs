using System;
using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetOperationsDashboard;

public sealed class GetOperationsDashboardValidator : AbstractValidator<GetOperationsDashboardQuery>
{
    public GetOperationsDashboardValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");
    }
}
