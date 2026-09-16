using System.Security.Cryptography;
using System.Text;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResolveOps.Domain;
using ResolveOps.Domain.Tracking;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Modules.Integrations.Carriers.DemoCarrier;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Integrations.Features.CarrierWebhook;

internal sealed class CarrierWebhookHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<CarrierWebhookHandler> _logger;

    private static readonly Action<ILogger, string, string, Exception?> _logWebhookReceived =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "WebhookReceived"),
            "Received tracking webhook for carrier {CarrierCode}, correlation {CorrelationId}");

    private static readonly Action<ILogger, string, string, Exception?> _logDuplicateIgnored =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2, "WebhookDuplicateIgnored"),
            "Duplicate webhook replay ignored for carrier {CarrierCode}, externalEventId {ExternalEventId}");

    private static readonly Action<ILogger, string, Guid, Exception?> _logReceiptAccepted =
        LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(3, "WebhookReceiptAccepted"),
            "Inbound tracking receipt accepted for carrier {CarrierCode}, receiptId {ReceiptId}");

    public CarrierWebhookHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        IOutboxWriter outboxWriter,
        ILogger<CarrierWebhookHandler> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<Result<CarrierWebhookResponse>> HandleAsync(
        string carrierCode,
        byte[] bodyBytes,
        string? signatureHeader,
        string? tenantIdHeader,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logWebhookReceived(_logger, carrierCode, correlationId, null);

        var normalizedCarrierCode = carrierCode.Trim().ToUpperInvariant();

        // ── 1. Validate signature ─────────────────────────────────────────────
        if (normalizedCarrierCode == DemoCarrierAdapter.CarrierCode)
        {
            var isValid = DemoCarrierAdapter.ValidateSignature(bodyBytes, signatureHeader);
            if (!isValid)
            {
                return DomainError.Failure(
                    "ERR_INVALID_SIGNATURE",
                    "The webhook request HMAC signature is missing or invalid.");
            }
        }

        var rawBody = Encoding.UTF8.GetString(bodyBytes);
        var parsedEvent = DemoCarrierAdapter.Parse(rawBody);

        // ── 2. Resolve Tenant and Carrier ─────────────────────────────────────
        Guid tenantId = Guid.Empty;
        Guid? carrierId = null;

        if (!string.IsNullOrWhiteSpace(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }

        // Try to look up carrier by code
        var carrier = await _dbContext.Carriers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Code == normalizedCarrierCode && (tenantId == Guid.Empty || c.TenantId == tenantId),
                cancellationToken);

        if (carrier != null)
        {
            carrierId = carrier.Id;
            if (tenantId == Guid.Empty)
            {
                tenantId = carrier.TenantId;
            }
        }

        // Fallback tenant if not resolved
        if (tenantId == Guid.Empty)
        {
            var defaultTenant = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Select(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            tenantId = defaultTenant;
        }

        var now = _timeProvider.GetUtcNow();
        var externalEventId = parsedEvent?.ExternalEventId;

        // ── 3. Duplicate / Replay Guard (spec §24 Phase 6 task 3, §26.2) ───────
        if (!string.IsNullOrWhiteSpace(externalEventId))
        {
            var existingReceipt = await _dbContext.InboundEventReceipts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.TenantId == tenantId
                         && r.SourceSystem == normalizedCarrierCode
                         && r.ExternalEventId == externalEventId,
                    cancellationToken);

            if (existingReceipt != null)
            {
                _logDuplicateIgnored(_logger, normalizedCarrierCode, externalEventId, null);
                return new CarrierWebhookResponse(
                    existingReceipt.Id,
                    existingReceipt.ProcessingStatus,
                    "Receipt already ingested (duplicate webhook replay ignored).");
            }
        }

        // ── 4. Persist Inbound Receipt durably ────────────────────────────────
        var payloadHash = Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLowerInvariant();

        var receipt = InboundEventReceipt.Create(
            tenantId: tenantId,
            carrierId: carrierId,
            sourceSystem: normalizedCarrierCode,
            externalEventId: externalEventId,
            idempotencyKey: null,
            payloadHash: payloadHash,
            rawPayload: rawBody,
            rawPayloadUri: null,
            receivedAtUtc: now,
            signatureValid: true,
            correlationId: correlationId);

        _dbContext.InboundEventReceipts.Add(receipt);

        // ── 5. Outbox: publish normalization request (spec §24 Phase 6 task 5) ──
        var outboxEvent = new TrackingIngestionRequestedV1
        {
            ReceiptId = receipt.Id,
        };

        _outboxWriter.Write(outboxEvent, tenantId, correlationId);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Race condition between duplicate webhook calls
            var existingReceipt = await _dbContext.InboundEventReceipts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.TenantId == tenantId
                         && r.SourceSystem == normalizedCarrierCode
                         && r.ExternalEventId == externalEventId,
                    cancellationToken);

            if (existingReceipt != null)
            {
                _logDuplicateIgnored(_logger, normalizedCarrierCode, externalEventId ?? string.Empty, null);
                return new CarrierWebhookResponse(
                    existingReceipt.Id,
                    existingReceipt.ProcessingStatus,
                    "Receipt already ingested (duplicate webhook replay ignored).");
            }

            throw;
        }

        TrackingMetrics.ReceiptsTotal.Add(1);
        _logReceiptAccepted(_logger, normalizedCarrierCode, receipt.Id, null);

        return new CarrierWebhookResponse(
            receipt.Id,
            InboundReceiptStatus.Received,
            "Tracking receipt accepted for asynchronous normalization.");
    }
}
