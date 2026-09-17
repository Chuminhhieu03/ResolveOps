using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Workflow;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Scheduled Quartz.NET job that scans active SLA clocks and marks overdue clocks as breached (spec §9.4, §18.1, §18.2, §24 Phase 8).
/// Emits CaseSlaBreachedV1 to outbox and logs timeline entries on affected cases.
/// </summary>
[DisallowConcurrentExecution]
public sealed class SlaBreachScanJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SlaBreachScanJob> _logger;

    private static readonly Action<ILogger, int, Exception?> _logScanCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "SlaScanCompleted"),
            "SlaBreachScanJob checked running SLA clocks. Found {BreachCount} breached clocks.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logClockBreached =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(2, "SlaClockBreached"),
            "SLA clock {ClockId} ({ClockType}) breached its deadline.");

    private static readonly Action<ILogger, Exception?> _logConcurrencyConflict =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3, "SlaScanConcurrencyConflict"),
            "Concurrency conflict during SlaBreachScanJob update. Overlapping scan or mutation occurred.");

    public SlaBreachScanJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<SlaBreachScanJob> logger)
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
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

        // Query overdue running clocks across all tenants (indexed query on tenant_id, status, due_at_utc)
        var overdueClocks = await dbContext.SlaClocks
            .IgnoreQueryFilters()
            .Where(c => c.Status == SlaClockStatus.Running && c.DueAtUtc != null && c.DueAtUtc <= now)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);

        if (overdueClocks.Count == 0)
        {
            _logScanCompleted(_logger, 0, null);
            return;
        }

        var breachedCount = 0;

        foreach (var clock in overdueClocks)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var breachResult = clock.Breach(_timeProvider);
            if (!breachResult.IsSuccess)
            {
                continue;
            }

            // Retrieve case to get severity and append timeline entry
            var exceptionCase = await dbContext.ExceptionCases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == clock.CaseId, cancellationToken);

            var severity = exceptionCase?.Severity ?? ExceptionSeverity.High;

            if (exceptionCase is not null)
            {
                var summary = $"SLA clock '{clock.ClockType}' breached target deadline {clock.DueAtUtc:u}.";
                var timelineEntry = CaseTimelineEntry.Create(
                    clock.TenantId,
                    clock.CaseId,
                    CaseTimelineEntryType.SlaBreached,
                    ActorType.System,
                    actorId: null,
                    summary: summary,
                    detailsJson: null,
                    correlationId: Guid.NewGuid().ToString("N"),
                    timeProvider: _timeProvider);

                dbContext.CaseTimelineEntries.Add(timelineEntry);
            }

            // Emit CaseSlaBreachedV1 to outbox (spec §17.3)
            var breachEvent = new CaseSlaBreachedV1
            {
                CaseId = clock.CaseId,
                ClockId = clock.Id,
                ClockType = clock.ClockType,
                BreachedAtUtc = now,
                Severity = severity
            };

            outboxWriter.Write(breachEvent, clock.TenantId, Guid.NewGuid().ToString("N"));

            _logClockBreached(_logger, clock.Id, clock.ClockType, null);
            WorkflowMetrics.SlaBreachedTotal.Add(1);
            breachedCount++;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logConcurrencyConflict(_logger, ex);
        }

        _logScanCompleted(_logger, breachedCount, null);
    }
}
