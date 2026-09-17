using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResolveOps.Application;
using ResolveOps.Domain.Workflow;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Services;

/// <summary>
/// Domain service coordinating SLA clock lifecycle transitions across exception workflows (spec §9.4, §15.8).
/// </summary>
public sealed class SlaClockService : ISlaClockService
{
    private readonly AppDbContext _dbContext;
    private readonly IBusinessCalendarService _calendarService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SlaClockService> _logger;

    private static readonly Action<ILogger, Guid, DateTimeOffset, DateTimeOffset, Exception?> _logClocksStarted =
        LoggerMessage.Define<Guid, DateTimeOffset, DateTimeOffset>(
            LogLevel.Information,
            new EventId(1, "SlaClocksStarted"),
            "Started SLA clocks for case {CaseId}. FirstAction due: {FirstActionDue:u}, Resolution due: {ResolutionDue:u}");

    private static readonly Action<ILogger, string, Guid, Exception?> _logClockCompleted =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Information,
            new EventId(2, "SlaClockCompleted"),
            "Completed SLA clock '{ClockType}' for case {CaseId}");

    private static readonly Action<ILogger, string, Guid, string, Exception?> _logClockPaused =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(3, "SlaClockPaused"),
            "Paused SLA clock '{ClockType}' for case {CaseId}. Reason: {ReasonCode}");

    private static readonly Action<ILogger, string, Guid, DateTimeOffset, Exception?> _logClockResumed =
        LoggerMessage.Define<string, Guid, DateTimeOffset>(
            LogLevel.Information,
            new EventId(4, "SlaClockResumed"),
            "Resumed SLA clock '{ClockType}' for case {CaseId}. New due date: {NewDueAtUtc:u}");

    public SlaClockService(
        AppDbContext dbContext,
        IBusinessCalendarService calendarService,
        TimeProvider timeProvider,
        ILogger<SlaClockService> logger)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartCaseClocksAsync(
        Guid tenantId,
        Guid caseId,
        Guid? slaPolicyVersionId,
        DateTimeOffset detectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve SLA Policy Version
        SlaPolicyVersion? policyVersion = null;
        if (slaPolicyVersionId.HasValue && slaPolicyVersionId.Value != Guid.Empty)
        {
            policyVersion = await _dbContext.SlaPolicyVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == slaPolicyVersionId.Value, cancellationToken);
        }

        if (policyVersion is null)
        {
            policyVersion = await _dbContext.SlaPolicyVersions
                .AsNoTracking()
                .Where(v => v.TenantId == tenantId && v.Status == "Active")
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var policyVersionId = policyVersion?.Id ?? Guid.Empty;
        var calendarId = policyVersion?.CalendarId;

        var firstActionMinutes = policyVersion?.FirstActionMinutes
            ?? policyVersion?.AcknowledgementMinutes
            ?? 60;

        var resolutionMinutes = policyVersion?.ResolutionMinutes
            ?? 1440; // 24 hours default

        // 2. Calculate deadlines
        var firstActionDue = await _calendarService.CalculateDeadlineAsync(
            tenantId,
            detectedAtUtc,
            firstActionMinutes,
            calendarId,
            cancellationToken);

        var resolutionDue = await _calendarService.CalculateDeadlineAsync(
            tenantId,
            detectedAtUtc,
            resolutionMinutes,
            calendarId,
            cancellationToken);

        // 3. Create FirstAction Clock
        var firstActionClock = SlaClock.Create(
            tenantId,
            caseId,
            SlaClockType.FirstAction,
            policyVersionId,
            _timeProvider);

        firstActionClock.Start(firstActionDue, _timeProvider);
        _dbContext.SlaClocks.Add(firstActionClock);

        // 4. Create Resolution Clock
        var resolutionClock = SlaClock.Create(
            tenantId,
            caseId,
            SlaClockType.Resolution,
            policyVersionId,
            _timeProvider);

        resolutionClock.Start(resolutionDue, _timeProvider);
        _dbContext.SlaClocks.Add(resolutionClock);

        _logClocksStarted(_logger, caseId, firstActionDue, resolutionDue, null);
    }

    public async Task CompleteClockAsync(
        Guid tenantId,
        Guid caseId,
        string clockType,
        CancellationToken cancellationToken = default)
    {
        var clock = await _dbContext.SlaClocks
            .Include(c => c.Pauses)
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId &&
                     c.CaseId == caseId &&
                     c.ClockType == clockType &&
                     (c.Status == SlaClockStatus.Running || c.Status == SlaClockStatus.Paused),
                cancellationToken);

        if (clock is not null)
        {
            clock.Complete(_timeProvider);
            _logClockCompleted(_logger, clockType, caseId, null);
        }
    }

    public async Task PauseClocksAsync(
        Guid tenantId,
        Guid caseId,
        string reasonCode,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var runningClocks = await _dbContext.SlaClocks
            .Where(c => c.TenantId == tenantId && c.CaseId == caseId && c.Status == SlaClockStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var clock in runningClocks)
        {
            clock.Pause(reasonCode, actorId, _timeProvider);
            _logClockPaused(_logger, clock.ClockType, caseId, reasonCode, null);
        }
    }

    public async Task ResumeClocksAsync(
        Guid tenantId,
        Guid caseId,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var pausedClocks = await _dbContext.SlaClocks
            .Include(c => c.Pauses)
            .Where(c => c.TenantId == tenantId && c.CaseId == caseId && c.Status == SlaClockStatus.Paused)
            .ToListAsync(cancellationToken);

        var now = _timeProvider.GetUtcNow();

        foreach (var clock in pausedClocks)
        {
            if (clock.PausedAtUtc.HasValue && clock.DueAtUtc.HasValue)
            {
                var pausedDuration = now - clock.PausedAtUtc.Value;
                if (pausedDuration > TimeSpan.Zero)
                {
                    var newDueAtUtc = await _calendarService.AddDurationAsync(
                        tenantId,
                        clock.DueAtUtc.Value,
                        pausedDuration,
                        calendarId: null,
                        cancellationToken);

                    clock.Resume(newDueAtUtc, actorId, _timeProvider);
                    _logClockResumed(_logger, clock.ClockType, caseId, newDueAtUtc, null);
                    continue;
                }
            }

            clock.Resume(clock.DueAtUtc ?? now, actorId, _timeProvider);
            _logClockResumed(_logger, clock.ClockType, caseId, clock.DueAtUtc ?? now, null);
        }
    }
}
