using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResolveOps.Application.Notifications;
using ResolveOps.Domain.Identity;
using ResolveOps.Domain.Messaging;
using ResolveOps.Domain.Notifications;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Messaging.Options;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Consumers;

/// <summary>
/// Background consumer that processes operational integration events and generates
/// in-app and email notifications with idempotency and preference filtering (spec §0 Rule 10, §12, §16, §17.3, §24 Phase 13).
/// </summary>
public sealed class NotificationConsumerService : BackgroundService
{
    private const string _queueName = "resolveops.notifications";
    private const string _consumerName = "NotificationConsumer";
    private const string _dlxExchangeName = "resolveops.dlx";
    private const string _deadLetterQueueName = "resolveops.dead-letter";
    private const string _deadLetterRoutingKey = "notifications.failed";

    private static readonly string[] _monitoredExchanges =
    [
        "CaseSlaBreachedV1",
        "ExceptionDetectedV1",
        "ClaimDecisionRecordedV1",
        "ClaimRecoveryRecordedV1",
        "ClaimSubmittedV1"
    ];

    private static readonly Action<ILogger, string, Exception?> _logChannelCreationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "ChannelCreationError"),
            "Failed to create RabbitMQ channel for {QueueName}");

    private static readonly Action<ILogger, ulong, Exception?> _logError =
        LoggerMessage.Define<ulong>(LogLevel.Error, new EventId(2, "ConsumerError"),
            "Error processing notification event delivery {DeliveryTag}");

    private static readonly Action<ILogger, string, Exception?> _logStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "ConsumerStarted"),
            "NotificationConsumerService started listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, "ConsumerStopped"),
            "NotificationConsumerService stopped listening on queue {QueueName}");

    private static readonly Action<ILogger, string, Exception?> _logDuplicateInbox =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5, "DuplicateInbox"),
            "Inbox duplicate message {MessageId} ignored.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logNoRecipients =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(6, "NoRecipients"),
            "No recipients found in tenant {TenantId} for event {EventType}");

    private static readonly Action<ILogger, ulong, string, Exception?> _logDeadLettered =
        LoggerMessage.Define<ulong, string>(LogLevel.Warning, new EventId(7, "MessageDeadLettered"),
            "Notification event delivery {DeliveryTag} routed to DLX due to: {Reason}");

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<RabbitMqConsumerOptions> _options;
    private readonly ILogger<NotificationConsumerService> _logger;

    public NotificationConsumerService(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<RabbitMqConsumerOptions> options,
        ILogger<NotificationConsumerService> logger)
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

            // 4. Declare main queue with DLX arguments
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

            // 5. Declare and bind monitored fanout exchanges
            foreach (var exchange in _monitoredExchanges)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: exchange,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: exchange,
                    routingKey: string.Empty,
                    cancellationToken: stoppingToken);
            }

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

        var messageId = $"{envelope.CorrelationId}_{envelope.EventType}_{_consumerName}";

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var realtimeService = scope.ServiceProvider.GetRequiredService<INotificationRealtimeService>();

        // ── Step 1: Transactional Inbox check ──────────────────────────────────
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingInbox = await dbContext.InboxMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.ConsumerName == _consumerName && m.MessageId == messageId, cancellationToken);

        if (existingInbox != null)
        {
            _logDuplicateInbox(_logger, messageId, null);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inboxRecord = InboxMessage.Create(
            _consumerName,
            messageId,
            envelope.TenantId,
            _timeProvider.GetUtcNow());

        inboxRecord.MarkProcessed(_timeProvider.GetUtcNow());
        dbContext.InboxMessages.Add(inboxRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // ── Step 2: Determine notification details ─────────────────────────────
        var details = await ExtractNotificationDetailsAsync(dbContext, envelope, cancellationToken);
        if (details == null || !envelope.TenantId.HasValue)
        {
            return;
        }

        var tenantId = envelope.TenantId.Value;

        // ── Step 3: Identify recipients ────────────────────────────────────────
        var usersQuery = from m in dbContext.UserTenantMemberships
                         join u in dbContext.Users on m.UserId equals u.Id
                         where m.TenantId == tenantId && m.Status == MembershipStatus.Active
                         select new
                         {
                             m.UserId,
                             u.Email,
                             u.UserName,
                             m.RolesJson
                         };

        var tenantUsers = await usersQuery.ToListAsync(cancellationToken);
        var matchingUsers = tenantUsers.Where(u =>
        {
            if (details.TargetUserIds.Count > 0 && details.TargetUserIds.Contains(u.UserId))
            {
                return true;
            }
            if (details.TargetRoles.Count == 0)
            {
                return true;
            }
            var roles = JsonSerializer.Deserialize<List<string>>(u.RolesJson) ?? [];
            return roles.Any(r => details.TargetRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        }).ToList();

        // Fallback: if no specific role matched, notify all active tenant users
        if (matchingUsers.Count == 0)
        {
            matchingUsers = tenantUsers;
        }

        if (matchingUsers.Count == 0)
        {
            _logNoRecipients(_logger, tenantId, envelope.EventType, null);
            return;
        }

        var userIds = matchingUsers.Select(u => u.UserId).ToList();
        var preferences = await dbContext.NotificationPreferences
            .Where(p => p.TenantId == tenantId && userIds.Contains(p.UserId) && p.NotificationClass == details.NotificationClass)
            .ToListAsync(cancellationToken);

        var isMandatory = NotificationClass.IsMandatory(details.NotificationClass);

        var emailsToSend = new List<(string Email, string Subject, string Body, string IdempotencyKey)>();

        // ── Step 4: Process In-App & Prepare Email (Transactional DB save) ───────
        foreach (var user in matchingUsers)
        {
            var inAppPref = preferences.FirstOrDefault(p => p.UserId == user.UserId && p.Channel == NotificationChannel.InApp);
            var emailPref = preferences.FirstOrDefault(p => p.UserId == user.UserId && p.Channel == NotificationChannel.Email);

            var allowInApp = isMandatory || (inAppPref?.IsEnabled ?? true);
            var allowEmail = isMandatory || (emailPref?.IsEnabled ?? true);

            Guid? notificationId = null;

            // In-App Notification
            if (allowInApp)
            {
                var inAppKey = $"{details.NotificationClass}:inapp:{envelope.CorrelationId}:{user.UserId}";
                var alreadyDelivered = await dbContext.NotificationDeliveries
                    .AnyAsync(d => d.TenantId == tenantId && d.IdempotencyKey == inAppKey, cancellationToken);

                if (!alreadyDelivered)
                {
                    var notificationResult = Notification.Create(
                        tenantId,
                        user.UserId,
                        details.NotificationClass,
                        NotificationChannel.InApp,
                        details.Title,
                        details.Message,
                        details.DataJson,
                        _timeProvider);

                    if (notificationResult.IsSuccess)
                    {
                        var notification = notificationResult.Value;
                        notificationId = notification.Id;
                        dbContext.Notifications.Add(notification);

                        var deliveryResult = NotificationDelivery.Create(
                            tenantId,
                            notification.Id,
                            NotificationChannel.InApp,
                            user.UserId.ToString(),
                            details.Title,
                            inAppKey,
                            _timeProvider);

                        if (deliveryResult.IsSuccess)
                        {
                            var delivery = deliveryResult.Value;
                            delivery.MarkSent(null, _timeProvider);
                            dbContext.NotificationDeliveries.Add(delivery);
                        }

                        NotificationMetrics.NotificationsSentTotal.Add(1, new KeyValuePair<string, object?>("channel", "in_app"));

                        // SignalR push
                        _ = realtimeService.SendNotificationToUserAsync(tenantId, user.UserId, new
                        {
                            notification.Id,
                            notification.NotificationClass,
                            notification.Channel,
                            notification.Title,
                            notification.Message,
                            notification.CreatedAtUtc
                        }, CancellationToken.None);
                    }
                }
            }

            // Email Notification Preparation
            if (allowEmail && !string.IsNullOrWhiteSpace(user.Email))
            {
                var emailKey = $"{details.NotificationClass}:email:{envelope.CorrelationId}:{user.UserId}";
                var alreadyDelivered = await dbContext.NotificationDeliveries
                    .AnyAsync(d => d.TenantId == tenantId && d.IdempotencyKey == emailKey, cancellationToken);

                if (!alreadyDelivered)
                {
                    var deliveryResult = NotificationDelivery.Create(
                        tenantId,
                        notificationId,
                        NotificationChannel.Email,
                        user.Email,
                        details.Title,
                        emailKey,
                        _timeProvider);

                    if (deliveryResult.IsSuccess)
                    {
                        dbContext.NotificationDeliveries.Add(deliveryResult.Value);
                        emailsToSend.Add((user.Email, details.Title, details.Message, emailKey));
                    }
                }
            }
        }

        // Commit database entities before external email calls (Rule 10)
        await dbContext.SaveChangesAsync(cancellationToken);

        // ── Step 5: Send Emails OUTSIDE Database Transaction (Rule 10) ─────────
        foreach (var (email, subject, emailBody, idempotencyKey) in emailsToSend)
        {
            var emailMessage = new EmailMessage(
                To: email,
                Subject: subject,
                BodyHtml: $"<html><body><h3>{subject}</h3><p>{emailBody}</p><hr/><p><small>ResolveOps Notifications</small></p></body></html>",
                BodyPlainText: emailBody);

            var sendResult = await emailSender.SendEmailAsync(emailMessage, cancellationToken);

            // Update delivery record status
            var delivery = await dbContext.NotificationDeliveries
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.IdempotencyKey == idempotencyKey, cancellationToken);

            if (delivery != null)
            {
                if (sendResult.Success)
                {
                    delivery.MarkSent(sendResult.ProviderMessageId, _timeProvider);
                }
                else
                {
                    delivery.RecordFailure(sendResult.ErrorMessage ?? "Email dispatch failure", canRetry: true, TimeSpan.FromMinutes(5), _timeProvider);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // ── Step 6: Broadcast realtime event to tenant ─────────────────────────
        await realtimeService.BroadcastToTenantAsync(tenantId, envelope.EventType, new
        {
            details.NotificationClass,
            details.Title,
            details.Message,
            envelope.CorrelationId
        }, cancellationToken);
    }

    private sealed record NotificationDetails(
        string NotificationClass,
        string Title,
        string Message,
        string? DataJson,
        IReadOnlyList<string> TargetRoles,
        IReadOnlyList<Guid> TargetUserIds);

    private static async Task<NotificationDetails?> ExtractNotificationDetailsAsync(
        AppDbContext dbContext,
        IntegrationEventEnvelope envelope,
        CancellationToken ct)
    {
        switch (envelope.EventType)
        {
            case "CaseSlaBreachedV1":
                {
                    var payload = JsonSerializer.Deserialize<CaseSlaBreachedV1>(envelope.Payload);
                    if (payload == null) return null;

                    var caseItem = await dbContext.ExceptionCases
                        .FirstOrDefaultAsync(c => c.Id == payload.CaseId, ct);

                    var caseNumber = caseItem?.CaseNumber ?? payload.CaseId.ToString()[..8];
                    var assignedUsers = caseItem?.OwnerUserId.HasValue == true
                        ? new[] { caseItem.OwnerUserId.Value }
                        : Array.Empty<Guid>();

                    var title = $"SLA Breach Alert: Case {caseNumber} ({payload.ClockType})";
                    var message = $"Case {caseNumber} breached its {payload.ClockType} deadline at {payload.BreachedAtUtc:u}. Severity: {payload.Severity}. Immediate action required.";

                    return new NotificationDetails(
                        NotificationClass.SlaBreachAlert,
                        title,
                        message,
                        envelope.Payload,
                        ["OperationsManager", "ExceptionSpecialist"],
                        assignedUsers);
                }

            case "ExceptionDetectedV1":
                {
                    var payload = JsonSerializer.Deserialize<ExceptionDetectedV1>(envelope.Payload);
                    if (payload == null) return null;

                    var title = $"Exception Escalation: {payload.CaseNumber} ({payload.ExceptionType})";
                    var message = $"Exception '{payload.ExceptionType}' with {payload.Severity} severity detected on case {payload.CaseNumber}.";

                    return new NotificationDetails(
                        NotificationClass.ExceptionEscalation,
                        title,
                        message,
                        envelope.Payload,
                        ["ExceptionSpecialist", "LogisticsCoordinator", "OperationsManager"],
                        []);
                }

            case "ClaimDecisionRecordedV1":
                {
                    var payload = JsonSerializer.Deserialize<ClaimDecisionRecordedV1>(envelope.Payload);
                    if (payload == null) return null;

                    var claim = await dbContext.Claims
                        .FirstOrDefaultAsync(c => c.Id == payload.ClaimId, ct);
                    var claimNumber = claim?.ClaimNumber ?? payload.ClaimId.ToString()[..8];

                    var title = $"Claim Decision Recorded: Claim {claimNumber}";
                    var message = $"Carrier recorded decision '{payload.Decision}' with approved amount {payload.ApprovedAmount:N2} {payload.Currency} for claim {claimNumber}.";

                    return new NotificationDetails(
                        NotificationClass.ClaimDecisionReceived,
                        title,
                        message,
                        envelope.Payload,
                        ["ClaimsSpecialist", "Finance"],
                        []);
                }

            case "ClaimRecoveryRecordedV1":
                {
                    var payload = JsonSerializer.Deserialize<ClaimRecoveryRecordedV1>(envelope.Payload);
                    if (payload == null) return null;

                    var claim = await dbContext.Claims
                        .FirstOrDefaultAsync(c => c.Id == payload.ClaimId, ct);
                    var claimNumber = claim?.ClaimNumber ?? payload.ClaimId.ToString()[..8];

                    var title = $"Recovery Recorded: Claim {claimNumber}";
                    var message = $"Recovery transaction '{payload.TransactionType}' of {payload.Amount:N2} {payload.Currency} (Ref: {payload.ExternalReference}) was recorded for claim {claimNumber}.";

                    return new NotificationDetails(
                        NotificationClass.ClaimRecoveryRecorded,
                        title,
                        message,
                        envelope.Payload,
                        ["Finance", "ClaimsSpecialist"],
                        []);
                }

            case "ClaimSubmittedV1":
                {
                    var payload = JsonSerializer.Deserialize<ClaimSubmittedV1>(envelope.Payload);
                    if (payload == null) return null;

                    var title = $"Claim Review Requested: Claim {payload.ClaimNumber}";
                    var message = $"Claim {payload.ClaimNumber} for {payload.ClaimedAmount:N2} {payload.Currency} was submitted to carrier. Carrier deadline: {payload.DeadlineAtUtc:u}.";

                    return new NotificationDetails(
                        NotificationClass.ClaimReviewRequested,
                        title,
                        message,
                        envelope.Payload,
                        ["ClaimsSpecialist", "OperationsManager"],
                        []);
                }

            default:
                return null;
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
