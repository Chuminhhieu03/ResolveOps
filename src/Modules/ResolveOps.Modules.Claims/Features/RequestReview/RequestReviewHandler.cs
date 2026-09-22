using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Claims.Readiness;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.RequestReview;

public class RequestReviewHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IClaimReadinessEvaluator _readinessEvaluator;
    private readonly TimeProvider _timeProvider;

    public RequestReviewHandler(
        AppDbContext dbContext,
        IClaimReadinessEvaluator readinessEvaluator,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _readinessEvaluator = readinessEvaluator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RequestReviewResponse>> HandleAsync(
        RequestReviewCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Approvals)
            .Include(c => c.LossComponents)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<RequestReviewResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var exceptionCase = await _dbContext.ExceptionCases
            .FirstOrDefaultAsync(c => c.Id == claim.CaseId, cancellationToken);

        if (exceptionCase == null)
        {
            return Result<RequestReviewResponse>.Failure(new DomainError("CASE_NOT_FOUND", "The associated exception case was not found."));
        }

        var readiness = await _readinessEvaluator.EvaluateAsync(claim, exceptionCase, cancellationToken);
        var hasMissingMandatoryEvidence = !readiness.IsReady;

        var reviewResult = claim.RequestReview(command.RequestedBy, hasMissingMandatoryEvidence, _timeProvider);
        if (reviewResult.IsFailure)
        {
            return Result<RequestReviewResponse>.Failure(reviewResult.Error);
        }

        var approval = reviewResult.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RequestReviewResponse>.Success(new RequestReviewResponse(
            claim.Id,
            claim.Status,
            approval.Id,
            approval.Status,
            approval.RequestedAtUtc));
    }
}
