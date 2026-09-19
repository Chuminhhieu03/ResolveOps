using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.ChangeSeverity;

public sealed class ChangeSeverityValidator : AbstractValidator<ChangeSeverityCommand>
{
    public ChangeSeverityValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty().Must(s => ExceptionSeverity.All.Contains(s))
            .WithMessage("Severity must be Low, Medium, High, or Critical.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
