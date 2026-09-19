using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Workflow;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.ResolveCase;

public sealed class ResolveCaseValidator : AbstractValidator<ResolveCaseCommand>
{
    public ResolveCaseValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
