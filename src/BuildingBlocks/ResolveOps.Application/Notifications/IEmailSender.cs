using System.Threading;
using System.Threading.Tasks;

namespace ResolveOps.Application.Notifications;

public interface IEmailSender
{
    Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken ct = default);
}
