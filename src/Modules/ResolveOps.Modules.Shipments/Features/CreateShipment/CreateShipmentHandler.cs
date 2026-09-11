using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Messaging;
using ResolveOps.Domain.Shipments;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Shipments.Features.CreateShipment;

internal sealed class CreateShipmentHandler
{
    private const string IdempotencyScope = "CreateShipment";
    private const int IdempotencyExpiryHours = 24;

    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxWriter _outboxWriter;

    public CreateShipmentHandler(
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

    public async Task<Result<CreateShipmentResponse>> HandleAsync(
        CreateShipmentCommand command,
        string? idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var now = _timeProvider.GetUtcNow();

        // ── Idempotency check (spec §24 Phase 4 task 6) ───────────────────────
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var requestHash = ComputeRequestHash(command);

            var existing = await _dbContext.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.TenantId == tenantId
                         && r.Scope == IdempotencyScope
                         && r.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (existing != null)
            {
                if (existing.RequestHash != requestHash)
                {
                    // Same key, different content → 409 Conflict per spec §15.11
                    return DomainError.Failure(
                        "ERR_IDEMPOTENCY_KEY_CONFLICT",
                        "The idempotency key was previously used with a different request body.");
                }

                // Exact replay → return original response
                var replayId = existing.ResourceId ?? Guid.Empty;
                return new CreateShipmentResponse(replayId);
            }
        }

        // ── Business invariant: unique (tenant, source_system, external_reference) ──
        var normalizedRef = command.ExternalReference.Trim();
        var normalizedSource = command.SourceSystem.Trim().ToUpperInvariant();

        var duplicateExists = await _dbContext.Shipments
            .AsNoTracking()
            .AnyAsync(
                s => s.TenantId == tenantId
                     && s.SourceSystem == normalizedSource
                     && s.ExternalReference == normalizedRef,
                cancellationToken);

        if (duplicateExists)
        {
            return DomainError.Failure(
                "ERR_SHIPMENT_DUPLICATE_REFERENCE",
                $"A shipment with external reference '{command.ExternalReference}' from source '{command.SourceSystem}' already exists.");
        }

        // ── Build aggregate ───────────────────────────────────────────────────
        var legs = command.Legs.Select(l => new LegDefinition(
            l.SequenceNumber,
            l.CarrierId,
            l.TrackingNumber,
            l.OriginLocationId,
            l.DestinationLocationId,
            l.PlannedDepartureAt,
            l.PlannedArrivalAt));

        var items = command.Items.Select(i => new ItemDefinition(
            i.LineReference,
            i.Sku,
            i.Description,
            i.ExpectedQuantity,
            i.QuantityUnit,
            i.UnitValue,
            i.Currency));

        var shipment = Shipment.Create(
            tenantId,
            command.ExternalReference,
            command.SourceSystem,
            command.CustomerId,
            command.OriginLocationId,
            command.DestinationLocationId,
            command.PlannedPickupAt,
            command.PlannedDeliveryAt,
            command.ServiceLevel,
            command.DeclaredValue,
            command.DeclaredValueCurrency,
            command.ExpectedPackageCount,
            command.ExpectedWeight,
            command.WeightUnit,
            legs,
            items,
            _timeProvider);

        _dbContext.Shipments.Add(shipment);

        // ── Outbox: ShipmentCreatedV1 in the same transaction (spec §24 Phase 4 task 8) ──
        var integrationEvent = new ShipmentCreatedV1
        {
            OccurredAtUtc = now,
            TenantId = tenantId,
            CorrelationId = correlationId,
            ShipmentId = shipment.Id,
            ExternalReference = shipment.ExternalReference,
            SourceSystem = shipment.SourceSystem,
            CustomerId = shipment.CustomerId,
            OriginLocationId = shipment.OriginLocationId,
            DestinationLocationId = shipment.DestinationLocationId,
            PlannedPickupAtUtc = shipment.PlannedPickupAtUtc,
            PlannedDeliveryAtUtc = shipment.PlannedDeliveryAtUtc,
            LegCount = shipment.Legs.Count,
        };

        _outboxWriter.Write(integrationEvent);

        // ── Idempotency record ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var requestHash = ComputeRequestHash(command);
            var response = new CreateShipmentResponse(shipment.Id);
            var responseBody = JsonSerializer.Serialize(response);

            var idempotencyRecord = IdempotencyRecord.Create(
                tenantId,
                IdempotencyScope,
                idempotencyKey,
                requestHash,
                201,
                responseBody,
                shipment.Id,
                now,
                now.AddHours(IdempotencyExpiryHours));

            _dbContext.IdempotencyRecords.Add(idempotencyRecord);
        }

        try
        {
            // Single transaction: shipment + outbox + idempotency record
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Race condition: another request beat us to the same unique reference.
            return DomainError.Failure(
                "ERR_SHIPMENT_DUPLICATE_REFERENCE",
                $"A shipment with external reference '{command.ExternalReference}' from source '{command.SourceSystem}' already exists.");
        }

        return new CreateShipmentResponse(shipment.Id);
    }

    private static string ComputeRequestHash(CreateShipmentCommand command)
    {
        var json = JsonSerializer.Serialize(command);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
