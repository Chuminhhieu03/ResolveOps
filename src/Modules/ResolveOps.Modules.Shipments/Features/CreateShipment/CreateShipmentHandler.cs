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
        string correlationId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var now = _timeProvider.GetUtcNow();

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

        _outboxWriter.Write(integrationEvent, tenantId, correlationId);

        try
        {
            // Single transaction: shipment + outbox
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
}
