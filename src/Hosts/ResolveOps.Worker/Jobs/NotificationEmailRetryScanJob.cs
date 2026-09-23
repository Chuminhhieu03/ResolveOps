using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ResolveOps.Application.Notifications;
using ResolveOps.Domain.Notifications;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

/// <summary>
/// Scheduled Quartz.NET job that scans pending/failed email notification deliveries
/// and retries dispatching with exponential backoff (spec §0 Rule 10, §12, §16, §24 Phase 13).
/// </summary>
[DisallowConcurrentExecution]
public sealed class NotificationEmailRetryScanJob : IJob
{
    private const int _maxAttempts = 5;
    private const int _batchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NotificationEmailRetryScanJob> _logger;

    private static readonly Action<ILogger, int, Exception?> _logScanStarted =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, "EmailRetryScanStarted"),
            "NotificationEmailRetryScanJob found {Count} deliveries eligible for retry.");

    private static readonly Action<ILogger, Guid, string, Exception?> _logEmailRetried =
        LoggerMessage.Define<Guid, string>(LogLevel.Information, new EventId(2, "EmailRetried"),
            "Notification delivery {DeliveryId} successfully resent to {Recipient}.");

    private static readonly Action<ILogger, Guid, string, int, Exception?> _logEmailRetryFailed =
        LoggerMessage.Define<Guid, string, int>(LogLevel.Warning, new EventId(3, "EmailRetryFailed"),
            "Notification delivery {DeliveryId} to {Recipient} failed on attempt {AttemptCount}.");

    public NotificationEmailRetryScanJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<NotificationEmailRetryScanJob> logger)
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
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var pendingDeliveries = await dbContext.NotificationDeliveries
            .IgnoreQueryFilters()
            .Where(d => d.Channel == NotificationChannel.Email &&
                        d.Status == DeliveryStatus.Retrying &&
                        d.NextAttemptAtUtc != null &&
                        d.NextAttemptAtUtc <= now)
            .OrderBy(d => d.NextAttemptAtUtc)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (pendingDeliveries.Count == 0)
        {
            return;
        }

        _logScanStarted(_logger, pendingDeliveries.Count, null);

        foreach (var delivery in pendingDeliveries)
        {
            var notification = delivery.NotificationId.HasValue
                ? await dbContext.Notifications.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.Id == delivery.NotificationId.Value, cancellationToken)
                : null;

            var bodyText = notification?.Message ?? delivery.Subject;
            var emailMessage = new EmailMessage(
                To: delivery.Recipient,
                Subject: delivery.Subject,
                BodyHtml: $"<html><body><h3>{delivery.Subject}</h3><p>{bodyText}</p><hr/><p><small>ResolveOps Notifications</small></p></body></html>",
                BodyPlainText: bodyText);

            // Rule 10: Email dispatch executed outside database transactions
            var sendResult = await emailSender.SendEmailAsync(emailMessage, cancellationToken);

            if (sendResult.Success)
            {
                delivery.MarkSent(sendResult.ProviderMessageId, _timeProvider);
                _logEmailRetried(_logger, delivery.Id, delivery.Recipient, null);
            }
            else
            {
                var nextAttemptCount = delivery.AttemptCount + 1;
                if (nextAttemptCount >= _maxAttempts)
                {
                    delivery.RecordFailure(sendResult.ErrorMessage ?? "Max retry attempts reached", canRetry: false, null, _timeProvider);
                }
                else
                {
                    var backoff = TimeSpan.FromMinutes(Math.Pow(2, delivery.AttemptCount));
                    delivery.RecordFailure(sendResult.ErrorMessage ?? "Email dispatch failure", canRetry: true, backoff, _timeProvider);
                }
                _logEmailRetryFailed(_logger, delivery.Id, delivery.Recipient, delivery.AttemptCount, null);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
