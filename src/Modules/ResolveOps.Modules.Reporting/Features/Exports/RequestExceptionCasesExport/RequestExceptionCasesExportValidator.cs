using System;
using FluentValidation;
using ResolveOps.Domain.Exceptions;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestExceptionCasesExport;

public sealed class RequestExceptionCasesExportValidator : AbstractValidator<RequestExceptionCasesExportCommand>
{
    public RequestExceptionCasesExportValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Severity)
            .Must(s => string.IsNullOrEmpty(s) || ExceptionSeverity.IsValid(s))
            .WithMessage("Invalid severity level.");

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}
