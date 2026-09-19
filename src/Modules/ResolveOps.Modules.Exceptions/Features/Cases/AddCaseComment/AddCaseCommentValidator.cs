using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.AddCaseComment;

public sealed class AddCaseCommentValidator : AbstractValidator<AddCaseCommentCommand>
{
    public AddCaseCommentValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
    }
}
