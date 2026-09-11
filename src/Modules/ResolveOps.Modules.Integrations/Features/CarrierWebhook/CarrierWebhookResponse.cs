namespace ResolveOps.Modules.Integrations.Features.CarrierWebhook;

/// <summary>
/// Response payload for carrier webhook ingestion.
/// </summary>
public sealed record CarrierWebhookResponse(
    Guid ReceiptId,
    string Status,
    string Message);
