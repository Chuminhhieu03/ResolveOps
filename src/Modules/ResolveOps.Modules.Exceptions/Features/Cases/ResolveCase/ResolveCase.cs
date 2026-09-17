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

public sealed record ResolveCaseRequest(
    string? ResolutionCode,
    string? RootCauseCode,
    string? DispositionCode,
    string Notes,
    string ConcurrencyStamp,
    string? DetailsJson = null);

public sealed record ResolveCaseCommand(
    Guid CaseId,
    string? ResolutionCode,
    string? RootCauseCode,
    string? DispositionCode,
    string Notes,
    string ConcurrencyStamp,
    string? DetailsJson = null);

public sealed class ResolveCaseValidator : AbstractValidator<ResolveCaseCommand>
{
    public ResolveCaseValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}

internal sealed class ResolveCaseHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ISlaClockService _slaClockService;
    private readonly TimeProvider _timeProvider;

    public ResolveCaseHandler(
        AppDbContext dbContext,
        ISlaClockService slaClockService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _slaClockService = slaClockService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        ResolveCaseCommand command,
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

        if (!string.Equals(exceptionCase.ConcurrencyStamp, command.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return DomainError.ConcurrencyConflict;
        }

        var resolveResult = exceptionCase.Resolve(
            command.ResolutionCode,
            command.RootCauseCode,
            command.DispositionCode,
            command.Notes,
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!resolveResult.IsSuccess)
        {
            return resolveResult;
        }

        // Complete Resolution SLA clock (spec §8.4, §9.4)
        await _slaClockService.CompleteClockAsync(
            exceptionCase.TenantId,
            exceptionCase.Id,
            SlaClockType.Resolution,
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            WorkflowMetrics.CaseTransitionsTotal.Add(1);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict;
        }

        return Result.Success();
    }
}

public sealed class ResolveCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{id:guid}/resolve", async (
            Guid id,
            ResolveCaseRequest request,
            IValidator<ResolveCaseCommand> validator,
            ResolveCaseHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new ResolveCaseCommand(
                id,
                request.ResolutionCode,
                request.RootCauseCode,
                request.DispositionCode,
                request.Notes,
                request.ConcurrencyStamp,
                request.DetailsJson);

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
        .WithName("ResolveCase")
        .WithTags("ExceptionCases")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUpdateCase);
    }
}
