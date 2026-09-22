using System.Collections.Generic;

namespace ResolveOps.Application.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    string? BodyPlainText = null,
    string? ReplyTo = null,
    IReadOnlyDictionary<string, string>? Headers = null);
