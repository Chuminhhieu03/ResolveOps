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

public sealed record AddCaseCommentRequest(string Comment);
public sealed record AddCaseCommentCommand(Guid CaseId, string Comment);

public sealed class AddCaseCommentValidator : AbstractValidator<AddCaseCommentCommand>
{
    public AddCaseCommentValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
    }
}

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

public sealed class AddCaseCommentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{id:guid}/comments", async (
            Guid id,
            AddCaseCommentRequest request,
            IValidator<AddCaseCommentCommand> validator,
            AddCaseCommentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCaseCommentCommand(id, request.Comment);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? currentUserId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : null;
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, currentUserId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("AddCaseComment")
        .WithTags("ExceptionCases")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireUpdateCase);
    }
}
