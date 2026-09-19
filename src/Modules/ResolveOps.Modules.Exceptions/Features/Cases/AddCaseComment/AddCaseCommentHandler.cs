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

internal sealed class AddCaseCommentHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AddCaseCommentHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        AddCaseCommentCommand command,
        Guid? currentUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var exceptionCase = await _dbContext.ExceptionCases
            .FirstOrDefaultAsync(c => c.Id == command.CaseId, cancellationToken);

        if (exceptionCase is null)
        {
            return DomainError.ResourceNotFound;
        }

        var commentResult = exceptionCase.AddComment(
            command.Comment,
            actorId: currentUserId,
            actorType: ActorType.User,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!commentResult.IsSuccess)
        {
            return commentResult;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
