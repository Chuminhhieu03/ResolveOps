using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Tracking;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tracking.Features.ManualIngest;

internal sealed class ManualIngestHandler
{
    private const string SourceSystemManual = "MANUAL";

    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxWriter _outboxWriter;

    public ManualIngestHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IOutboxWriter outboxWriter)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _outboxWriter = outboxWriter;
    }

    public async Task<Result<ManualIngestResponse>> HandleAsync(
        ManualIngestCommand command,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var now = _timeProvider.GetUtcNow();

        // Verify carrier exists
        var carrierExists = await _dbContext.Carriers
            .AsNoTracking()
            .AnyAsync(c => c.Id == command.CarrierId && c.TenantId == tenantId, cancellationToken);

        if (!carrierExists)
        {
            return DomainError.Failure(
                "ERR_CARRIER_NOT_FOUND",
                $"Carrier with ID '{command.CarrierId}' was not found.");
        }

        var json = JsonSerializer.Serialize(command);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        var externalEventId = Guid.NewGuid().ToString("N");

        var receipt = InboundEventReceipt.Create(
            tenantId: tenantId,
            carrierId: command.CarrierId,
            sourceSystem: SourceSystemManual,
            externalEventId: externalEventId,
            idempotencyKey: null,
            payloadHash: payloadHash,
            rawPayload: json,
            rawPayloadUri: null,
            receivedAtUtc: now,
            signatureValid: true,
            correlationId: correlationId);

        _dbContext.InboundEventReceipts.Add(receipt);

        var outboxEvent = new TrackingIngestionRequestedV1
        {
            OccurredAtUtc = now,
            TenantId = tenantId,
            CorrelationId = correlationId,
            ReceiptId = receipt.Id,
        };

        _outboxWriter.Write(outboxEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TrackingMetrics.ReceiptsTotal.Add(1);

        return new ManualIngestResponse(
            receipt.Id,
            InboundReceiptStatus.Received,
            "Manual tracking event accepted for normalization.");
    }
}
