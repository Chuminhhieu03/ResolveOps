namespace ResolveOps.Application.Notifications;

public sealed record EmailSendResult(
    bool Success,
    string? ProviderMessageId = null,
    string? ErrorMessage = null);
