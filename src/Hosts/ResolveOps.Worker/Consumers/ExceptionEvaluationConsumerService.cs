using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Messaging;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Messaging.Options;
using ResolveOps.Modules.Exceptions.Services;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Consumers;

/// <summary>
/// Background consumer that evaluates incoming TrackingEventAcceptedV1 integration events
/// against active exception policies (spec §8.3, §17.3, §17.4, §17.5, §24 Phase 7).
/// </summary>
public sealed class ExceptionEvaluationConsumerService : BackgroundService
{
    private const string _queueName = "resolveops.exception-evaluation";
    private const string _exchangeName = "TrackingEventAcceptedV1";
    private const string _consumerName = "ExceptionEvaluationConsumer";
    private const string _dlxExchangeName = "resolveops.dlx";
    private const string _deadLetterQueueName = "resolveops.dead-letter";
    private const string _deadLetterRoutingKey = "exception-evaluation.failed";

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<RabbitMqConsumerOptions> _options;
    private readonly ILogger<ExceptionEvaluationConsumerService> _logger;

    private static readonly Action<ILogger, string, Exception?> _logStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "ConsumerStarted"),
            "ExceptionEvaluationConsumerService started listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "ConsumerStopped"),
            "ExceptionEvaluationConsumerService stopped listening on queue {QueueName}");

    private static readonly Action<ILogger, string, string, Exception?> _logDuplicateInbox =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(3, "InboxDuplicate"),
            "Inbox duplicate message {MessageId} for consumer {ConsumerName} ignored.");

    private static readonly Action<ILogger, string, string, Exception?> _logCaseCreated =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(4, "ExceptionCaseCreated"),
            "Exception case created: {CaseNumber} ({ExceptionType})");

    private static readonly Action<ILogger, string, Guid, Exception?> _logOccurrenceAdded =
        LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(5, "OccurrenceAdded"),
            "Occurrence appended to active case {CaseNumber} for tracking event {TrackingEventId}");

    private static readonly Action<ILogger, ulong, Exception?> _logError =
        LoggerMessage.Define<ulong>(LogLevel.Error, new EventId(6, "ConsumerError"),
            "Error evaluating tracking event delivery {DeliveryTag}");

    private static readonly Action<ILogger, string, Exception?> _logChannelCreationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, "ChannelCreationError"),
            "Failed to create RabbitMQ channel for {QueueName}");

    private static readonly Action<ILogger, ulong, string, Exception?> _logDeadLettered =
        LoggerMessage.Define<ulong, string>(LogLevel.Warning, new EventId(8, "MessageDeadLettered"),
            "Tracking evaluation delivery {DeliveryTag} routed to DLX due to: {Reason}");

    public ExceptionEvaluationConsumerService(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<RabbitMqConsumerOptions> options,
        ILogger<ExceptionEvaluationConsumerService> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel channel;
        try
        {
            channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logChannelCreationError(_logger, _queueName, ex);
            return;
        }

        await using (channel)
        {
            // 1. Declare DLX (direct)
            await channel.ExchangeDeclareAsync(
                exchange: _dlxExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // 2. Declare dead-letter queue
            await channel.QueueDeclareAsync(
                queue: _deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // 3. Bind dead-letter queue to DLX
            await channel.QueueBindAsync(
                queue: _deadLetterQueueName,
                exchange: _dlxExchangeName,
                routingKey: _deadLetterRoutingKey,
                cancellationToken: stoppingToken);

            // 4. Declare main fanout exchange for TrackingEventAcceptedV1
            await channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // 5. Declare durable queue with DLX
            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _dlxExchangeName,
                ["x-dead-letter-routing-key"] = _deadLetterRoutingKey,
            };

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: stoppingToken);

            // 6. Bind queue to fanout exchange
            await channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: string.Empty,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.Value.PrefetchCount,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var deliveryTag = ea.DeliveryTag;

                try
                {
                    await ProcessMessageAsync(ea.Body.ToArray(), ea.BasicProperties.CorrelationId, stoppingToken);
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logError(_logger, deliveryTag, ex);

                    var isTransient = IsTransientError(ex);
                    var deathCount = GetDeathCount(ea.BasicProperties.Headers);

                    if (isTransient && deathCount < _options.Value.RetryLimit)
                    {
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                    }
                    else
                    {
                        _logDeadLettered(_logger, deliveryTag, isTransient ? "RetryLimitExceeded" : "NonRetryableError", null);
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: CancellationToken.None);
                    }
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logStarted(_logger, _queueName, null);

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (stoppingToken.Register(state => ((TaskCompletionSource)state!).TrySetResult(), tcs))
            {
                await tcs.Task;
            }

            _logStopped(_logger, _queueName, null);
        }
    }

    private async Task ProcessMessageAsync(byte[] body, string? correlationId, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IntegrationEventEnvelope: payload is null.");

        var payload = JsonSerializer.Deserialize<TrackingEventAcceptedV1>(envelope.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize TrackingEventAcceptedV1 payload: payload is null.");

        var messageId = $"{payload.TrackingEventId}_{_consumerName}";

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IExceptionPolicyEvaluator>();
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
        var slaClockService = scope.ServiceProvider.GetRequiredService<ResolveOps.Application.ISlaClockService>();
        var now = _timeProvider.GetUtcNow();

        // ── Transactional Inbox Check (spec §17.5) ───────────────────────────
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingInbox = await dbContext.InboxMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.ConsumerName == _consumerName && m.MessageId == messageId, cancellationToken);

        if (existingInbox != null)
        {
            _logDuplicateInbox(_logger, messageId, _consumerName, null);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inboxRecord = InboxMessage.Create(
            consumerName: _consumerName,
            messageId: messageId,
            tenantId: envelope.TenantId,
            receivedAtUtc: now);

        dbContext.InboxMessages.Add(inboxRecord);

        // ── Read TrackingEvent and Shipment ──────────────────────────────────
        var trackingEvent = await dbContext.TrackingEvents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == payload.TrackingEventId, cancellationToken);

        if (trackingEvent is null)
        {
            inboxRecord.MarkProcessed(now, resultHash: "TRACKING_EVENT_NOT_FOUND");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var shipment = await dbContext.Shipments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == payload.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            inboxRecord.MarkProcessed(now, resultHash: "SHIPMENT_NOT_FOUND");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        // ── Evaluate rules ───────────────────────────────────────────────────
        var stopwatch = Stopwatch.StartNew();
        var candidate = await evaluator.EvaluateTrackingEventAsync(trackingEvent, shipment, cancellationToken);
        stopwatch.Stop();
        ExceptionMetrics.DetectionDurationMs.Record(stopwatch.ElapsedMilliseconds);

        if (candidate is not null && candidate.IsViolation)
        {
            // Check if an active case already exists for this fingerprint
            var activeCase = await dbContext.ExceptionCases
                .IgnoreQueryFilters()
                .Include(c => c.Occurrences)
                .FirstOrDefaultAsync(
                    c => c.TenantId == shipment.TenantId &&
                         c.Fingerprint == candidate.Fingerprint &&
                         c.Status != ExceptionCaseStatus.Closed &&
                         c.Status != ExceptionCaseStatus.Cancelled,
                    cancellationToken);

            if (activeCase is not null)
            {
                activeCase.AddOccurrence(
                    trackingEvent.Id,
                    candidate.ExceptionType,
                    trackingEvent.OccurredAtUtc,
                    candidate.Summary,
                    _timeProvider);

                _logOccurrenceAdded(_logger, activeCase.CaseNumber, trackingEvent.Id, null);
            }
            else
            {
                var caseNumber = $"EXC-{now.Year}-{RandomNumberGenerator.GetInt32(100000, 999999)}";

                var newCase = ExceptionCase.Create(
                    shipment.TenantId,
                    caseNumber,
                    shipment.Id,
                    trackingEvent.ShipmentLegId,
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
                    correlationId: envelope.CorrelationId,
                    actorId: null,
                    actorType: ActorType.System);

                newCase.AddOccurrence(
                    trackingEvent.Id,
                    candidate.ExceptionType,
                    trackingEvent.OccurredAtUtc,
                    candidate.Summary,
                    _timeProvider);

                dbContext.ExceptionCases.Add(newCase);

                // Write ExceptionDetectedV1 to outbox atomically (spec §17.3)
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

                outboxWriter.Write(detectedEvent, shipment.TenantId, envelope.CorrelationId);

                // Start SLA clocks for the case
                await slaClockService.StartCaseClocksAsync(
                    shipment.TenantId,
                    newCase.Id,
                    null,
                    newCase.DetectedAtUtc,
                    cancellationToken);

                _logCaseCreated(_logger, newCase.CaseNumber, newCase.ExceptionType, null);
                ExceptionMetrics.ExceptionsDetectedTotal.Add(1);
            }
        }

        inboxRecord.MarkProcessed(now, resultHash: candidate is not null ? "EXCEPTION_EVALUATED" : "NO_VIOLATION");

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race condition check: filtered unique index UIX_ExceptionCases_ActiveFingerprint
            // If another process inserted a case with the same active fingerprint, safely rollback
            // and attach occurrence in a fresh transaction.
            await transaction.RollbackAsync(CancellationToken.None);
            await HandleRaceConditionAsync(payload, envelope, cancellationToken);
        }
    }

    private async Task HandleRaceConditionAsync(
        TrackingEventAcceptedV1 payload,
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingCase = await dbContext.ExceptionCases
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.ShipmentId == payload.ShipmentId &&
                     c.Status != ExceptionCaseStatus.Closed &&
                     c.Status != ExceptionCaseStatus.Cancelled,
                cancellationToken);

        if (existingCase is not null)
        {
            existingCase.AddOccurrence(
                payload.TrackingEventId,
                payload.NormalizedEventType,
                _timeProvider.GetUtcNow(),
                $"Race resolved: subsequent occurrence attached for tracking event {payload.TrackingEventId}.",
                _timeProvider);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsTransientError(Exception ex) =>
        ex is TimeoutException ||
        ex is System.IO.IOException ||
        ex is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 1205 || sqlEx.Number == -2);

    private static int GetDeathCount(IDictionary<string, object?>? headers)
    {
        if (headers == null || !headers.TryGetValue("x-death", out var deathObj))
        {
            return 0;
        }

        if (deathObj is List<object> deathList && deathList.Count > 0 &&
            deathList[0] is Dictionary<string, object> deathDict &&
            deathDict.TryGetValue("count", out var countObj) &&
            countObj is long count)
        {
            return (int)count;
        }

        return 0;
    }
}
