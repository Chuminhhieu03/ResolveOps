using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResolveOps.Application.Notifications;
using ResolveOps.Observability;

namespace ResolveOps.Modules.Notifications.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    private static readonly Action<ILogger, string, string, Exception?> _logEmailSent =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "EmailSent"),
            "Successfully sent email to {Recipient} with subject '{Subject}'");

    private static readonly Action<ILogger, string, string, Exception?> _logEmailFailed =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(2, "EmailFailed"),
            "Failed to send email to {Recipient} with subject '{Subject}'");

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName),
                Subject = message.Subject,
                Body = message.BodyHtml,
                IsBodyHtml = true
            };

            mailMessage.To.Add(message.To);

            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                mailMessage.ReplyToList.Add(message.ReplyTo);
            }

            if (message.Headers != null)
            {
                foreach (var (key, value) in message.Headers)
                {
                    mailMessage.Headers.Add(key, value);
                }
            }

            await client.SendMailAsync(mailMessage, ct);

            stopwatch.Stop();
            NotificationMetrics.EmailDurationSeconds.Record(stopwatch.Elapsed.TotalSeconds);
            NotificationMetrics.NotificationsSentTotal.Add(1, new KeyValuePair<string, object?>("channel", "email"));

            var providerMessageId = Guid.NewGuid().ToString("N");
            _logEmailSent(_logger, message.To, message.Subject, null);

            return new EmailSendResult(true, providerMessageId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            NotificationMetrics.EmailDurationSeconds.Record(stopwatch.Elapsed.TotalSeconds);
            NotificationMetrics.NotificationsFailedTotal.Add(1, new KeyValuePair<string, object?>("channel", "email"));

            _logEmailFailed(_logger, message.To, message.Subject, ex);
            return new EmailSendResult(false, null, ex.Message);
        }
    }
}
