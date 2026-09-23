using System;
using FluentValidation;

namespace ResolveOps.Modules.Reporting.Features.Exports.GetExportStatus;

public sealed class GetExportStatusValidator : AbstractValidator<GetExportStatusQuery>
{
    public GetExportStatusValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ExportId)
            .NotEmpty()
            .WithMessage("Export ID is required.");
    }
}
