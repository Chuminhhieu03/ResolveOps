using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Workflow;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Scheduled Quartz.NET job that scans claims needing carrier follow-up or additional info (spec §8.8, §8.9, §18.2, Phase 11).
/// Checks Submitted/UnderReview claims approaching follow-up response SLAs,
/// and MoreInformationRequested claims approaching carrier deadlines.
/// Generates operational reminder tasks idempotently.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ClaimFollowUpScanJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClaimFollowUpScanJob> _logger;

    private static readonly Action<ILogger, int, Exception?> _logScanCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "ClaimFollowUpScanCompleted"),
            "ClaimFollowUpScanJob finished scan. Created {TaskCount} operational follow-up tasks.");

    private static readonly Action<ILogger, Guid, string, string, Exception?> _logFollowUpNeeded =
        LoggerMessage.Define<Guid, string, string>(LogLevel.Warning, new EventId(2, "ClaimFollowUpNeeded"),
            "Claim {ClaimId} ({ClaimNumber}) requires operational action for state: {ClaimStatus}.");

    public ClaimFollowUpScanJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<ClaimFollowUpScanJob> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var now = _timeProvider.GetUtcNow();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newTasksCount = 0;

        // 1. Scan claims in Submitted or UnderReview where submitted over 7 days ago
        var submittedCutoff = now.AddDays(-7);
        var pendingDecisionClaims = await dbContext.Claims
            .IgnoreQueryFilters()
            .Where(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview) &&
                        c.SubmittedAtUtc != null &&
                        c.SubmittedAtUtc <= submittedCutoff)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var claim in pendingDecisionClaims)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var taskExists = await dbContext.WorkflowTasks
                .IgnoreQueryFilters()
                .AnyAsync(t => t.ClaimId == claim.Id &&
                               t.TaskType == WorkflowTaskType.ClaimFollowUp &&
                               t.Status != WorkflowTaskStatus.Completed &&
                               t.Status != WorkflowTaskStatus.Cancelled,
                    cancellationToken);

            if (!taskExists)
            {
                _logFollowUpNeeded(_logger, claim.Id, claim.ClaimNumber, claim.Status, null);

                var task = WorkflowTask.Create(
                    tenantId: claim.TenantId,
                    caseId: claim.CaseId,
                    taskType: WorkflowTaskType.ClaimFollowUp,
                    title: $"Follow-up on submitted claim {claim.ClaimNumber}",
                    description: $"Carrier has not provided a decision on claim {claim.ClaimNumber} submitted on {claim.SubmittedAtUtc:d}.",
                    priority: WorkflowTaskPriority.Normal,
                    ownerUserId: null,
                    ownerTeamCode: null,
                    dueAtUtc: now.AddDays(2),
                    isMandatory: false,
                    createdByPolicyId: null,
                    timeProvider: _timeProvider,
                    claimId: claim.Id);

                dbContext.WorkflowTasks.Add(task);
                newTasksCount++;
            }
        }

        // 2. Scan claims in MoreInformationRequested
        var moreInfoClaims = await dbContext.Claims
            .IgnoreQueryFilters()
            .Where(c => c.Status == ClaimStatus.MoreInformationRequested)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var claim in moreInfoClaims)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var taskExists = await dbContext.WorkflowTasks
                .IgnoreQueryFilters()
                .AnyAsync(t => t.ClaimId == claim.Id &&
                               t.TaskType == WorkflowTaskType.SupplyInformation &&
                               t.Status != WorkflowTaskStatus.Completed &&
                               t.Status != WorkflowTaskStatus.Cancelled,
                    cancellationToken);

            if (!taskExists)
            {
                _logFollowUpNeeded(_logger, claim.Id, claim.ClaimNumber, claim.Status, null);

                var task = WorkflowTask.Create(
                    tenantId: claim.TenantId,
                    caseId: claim.CaseId,
                    taskType: WorkflowTaskType.SupplyInformation,
                    title: $"Supply requested information for claim {claim.ClaimNumber}",
                    description: $"Carrier requested additional information for claim {claim.ClaimNumber}. Please supply the required details before deadline.",
                    priority: WorkflowTaskPriority.High,
                    ownerUserId: null,
                    ownerTeamCode: null,
                    dueAtUtc: now.AddDays(3),
                    isMandatory: true,
                    createdByPolicyId: null,
                    timeProvider: _timeProvider,
                    claimId: claim.Id);

                dbContext.WorkflowTasks.Add(task);
                newTasksCount++;
            }
        }

        if (newTasksCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logScanCompleted(_logger, newTasksCount, null);
    }
}
