using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Shipments;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Modules.Exceptions.Services;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Scheduled Quartz.NET job that scans shipments for missed planned pickup times and delivery milestones
/// against configured tolerance policies (spec §8.3, §18.2, §24 Phase 7, §26.4).
/// </summary>
[DisallowConcurrentExecution]
public sealed class MissedDeadlineScanJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MissedDeadlineScanJob> _logger;

    private static readonly Action<ILogger, int, Exception?> _logScanCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "ScanCompleted"),
            "MissedDeadlineScanJob completed evaluation of {Count} candidate shipments.");

    private static readonly Action<ILogger, string, string, Exception?> _logCaseCreated =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2, "CaseDetected"),
            "Missed deadline scan detected exception: case {CaseNumber} ({ExceptionType})");

    public MissedDeadlineScanJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<MissedDeadlineScanJob> logger)
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
        var evaluator = scope.ServiceProvider.GetRequiredService<IExceptionPolicyEvaluator>();
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
        var slaClockService = scope.ServiceProvider.GetRequiredService<ResolveOps.Application.ISlaClockService>();

        // Query active shipments needing pickup or delivery
        var candidateShipments = await dbContext.Shipments
            .IgnoreQueryFilters()
            .Where(s => s.Status == ShipmentStatus.Active || s.Status == ShipmentStatus.InTransit)
            .ToListAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        foreach (var shipment in candidateShipments)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // 1. Evaluate PickupDelay
            var pickupCandidate = await evaluator.EvaluatePickupDelayAsync(shipment, now, cancellationToken);
            if (pickupCandidate is not null && pickupCandidate.IsViolation)
            {
                await TryCreateCaseAsync(dbContext, outboxWriter, slaClockService, shipment, pickupCandidate, now, cancellationToken);
            }

            // 2. Evaluate InTransitDelay
            var inTransitCandidate = await evaluator.EvaluateInTransitDelayAsync(shipment, now, cancellationToken);
            if (inTransitCandidate is not null && inTransitCandidate.IsViolation)
            {
                await TryCreateCaseAsync(dbContext, outboxWriter, slaClockService, shipment, inTransitCandidate, now, cancellationToken);
            }
        }

        stopwatch.Stop();
        ExceptionMetrics.DetectionDurationMs.Record(stopwatch.ElapsedMilliseconds);
        _logScanCompleted(_logger, candidateShipments.Count, null);
    }

    private async Task TryCreateCaseAsync(
        AppDbContext dbContext,
        IOutboxWriter outboxWriter,
        ResolveOps.Application.ISlaClockService slaClockService,
        Shipment shipment,
        DetectionCandidate candidate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // 1. Check if active case with this fingerprint already exists
        var existingActive = await dbContext.ExceptionCases
            .IgnoreQueryFilters()
            .AnyAsync(
                c => c.TenantId == shipment.TenantId &&
                     c.Fingerprint == candidate.Fingerprint &&
                     c.Status != ExceptionCaseStatus.Closed &&
                     c.Status != ExceptionCaseStatus.Cancelled,
                cancellationToken);

        if (existingActive)
        {
            return; // Duplicate active case prevented (spec §10.3 invariant 1)
        }

        var caseNumber = $"EXC-{now.Year}-{RandomNumberGenerator.GetInt32(100000, 999999)}";

        var newCase = ExceptionCase.Create(
            shipment.TenantId,
            caseNumber,
            shipment.Id,
            shipmentLegId: null,
            candidate.ExceptionType,
            candidate.Fingerprint,
            candidate.Severity,
            candidate.SeverityScore,
            candidate.PolicyId,
            candidate.PolicyVersionNumber,
            candidate.OwnerTeamCode,
            candidate.FinancialExposure,
            candidate.ExposureCurrency,
            detectedAtUtc: now,
            timeProvider: _timeProvider,
            initialSummary: candidate.Summary,
            correlationId: Guid.NewGuid().ToString("N"),
            actorId: null,
            actorType: ActorType.System);

        dbContext.ExceptionCases.Add(newCase);

        var detectedEvent = new ExceptionDetectedV1
        {
            CaseId = newCase.Id,
            CaseNumber = newCase.CaseNumber,
            ShipmentId = newCase.ShipmentId,
            ExceptionType = newCase.ExceptionType,
            Severity = newCase.Severity,
            DetectedAtUtc = newCase.DetectedAtUtc,
            OwnerTeamCode = newCase.OwnerTeamCode
        };

        outboxWriter.Write(detectedEvent, shipment.TenantId, Guid.NewGuid().ToString("N"));

        // Start SLA clocks for the detected case
        await slaClockService.StartCaseClocksAsync(
            shipment.TenantId,
            newCase.Id,
            null,
            now,
            cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logCaseCreated(_logger, newCase.CaseNumber, newCase.ExceptionType, null);
            ExceptionMetrics.ExceptionsDetectedTotal.Add(1);
        }
        catch (DbUpdateException)
        {
            // Concurrent race condition safely handled via UIX_ExceptionCases_ActiveFingerprint
            // Another thread or event consumer inserted the case simultaneously
            dbContext.Entry(newCase).State = EntityState.Detached;
        }
    }
}
