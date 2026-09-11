namespace ResolveOps.Domain.Tracking;

/// <summary>
/// Represents a raw receipt of an inbound tracking event from an external carrier webhook or manual ingest (spec §15.6).
///
/// Invariants:
/// - (tenant_id, source_system, external_event_id) is unique when external_event_id is present.
/// - Persisted durably before any asynchronous normalization work is dispatched.
/// </summary>
public sealed class InboundEventReceipt
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CarrierId { get; private set; }
    public string SourceSystem { get; private set; } = string.Empty;
    public string? ExternalEventId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string PayloadHash { get; private set; } = string.Empty;
    public string? RawPayloadUri { get; private set; }
    public string RawPayload { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public bool? SignatureValid { get; private set; }
    public string ProcessingStatus { get; private set; } = InboundReceiptStatus.Received;
    public string? FailureCode { get; private set; }
    public string? FailureDetail { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    // EF Core requires a parameterless constructor
    private InboundEventReceipt() { }

    public static InboundEventReceipt Create(
        Guid tenantId,
        Guid? carrierId,
        string sourceSystem,
        string? externalEventId,
        string? idempotencyKey,
        string payloadHash,
        string rawPayload,
        string? rawPayloadUri,
        DateTimeOffset receivedAtUtc,
        bool? signatureValid,
        string correlationId)
    {
        return new InboundEventReceipt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CarrierId = carrierId,
            SourceSystem = sourceSystem.Trim().ToUpperInvariant(),
            ExternalEventId = string.IsNullOrWhiteSpace(externalEventId) ? null : externalEventId.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            PayloadHash = payloadHash,
            RawPayload = rawPayload,
            RawPayloadUri = rawPayloadUri,
            ReceivedAtUtc = receivedAtUtc,
            SignatureValid = signatureValid,
            ProcessingStatus = InboundReceiptStatus.Received,
            CorrelationId = correlationId,
        };
    }

    public void MarkNormalized()
    {
        ProcessingStatus = InboundReceiptStatus.Normalized;
        FailureCode = null;
        FailureDetail = null;
    }

    public void MarkQuarantined(string reasonCode, string detail)
    {
        ProcessingStatus = InboundReceiptStatus.Quarantined;
        FailureCode = reasonCode;
        FailureDetail = detail;
    }

    public void MarkFailed(string failureCode, string failureDetail)
    {
        ProcessingStatus = InboundReceiptStatus.Failed;
        FailureCode = failureCode;
        FailureDetail = failureDetail;
    }
}
