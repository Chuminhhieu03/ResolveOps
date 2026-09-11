using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Shipments.Features.GetShipment;

internal sealed class GetShipmentHandler
{
    private readonly AppDbContext _dbContext;

    public GetShipmentHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ShipmentDetailResponse>> HandleAsync(
        GetShipmentQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant query filter is applied automatically by AppDbContext.
        // No need to filter by TenantId explicitly — defense in depth.
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Legs)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == query.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            return DomainError.ResourceNotFound;
        }

        // Explicit mapping — no AutoMapper (spec §0.1 rule: no AutoMapper)
        var response = new ShipmentDetailResponse(
            Id: shipment.Id,
            ExternalReference: shipment.ExternalReference,
            SourceSystem: shipment.SourceSystem,
            CustomerId: shipment.CustomerId,
            OriginLocationId: shipment.OriginLocationId,
            DestinationLocationId: shipment.DestinationLocationId,
            Status: shipment.Status,
            ServiceLevel: shipment.ServiceLevel,
            PlannedPickupAtUtc: shipment.PlannedPickupAtUtc,
            PlannedDeliveryAtUtc: shipment.PlannedDeliveryAtUtc,
            ActualPickupAtUtc: shipment.ActualPickupAtUtc,
            ActualDeliveryAtUtc: shipment.ActualDeliveryAtUtc,
            DeclaredValue: shipment.DeclaredValue,
            DeclaredValueCurrency: shipment.DeclaredValueCurrency,
            ExpectedPackageCount: shipment.ExpectedPackageCount,
            ExpectedWeight: shipment.ExpectedWeight,
            WeightUnit: shipment.WeightUnit,
            Legs: [.. shipment.Legs.Select(l => new ShipmentLegResponse(
                l.Id,
                l.SequenceNumber,
                l.CarrierId,
                l.TrackingNumber,
                l.OriginLocationId,
                l.DestinationLocationId,
                l.PlannedDepartureAtUtc,
                l.PlannedArrivalAtUtc,
                l.ActualDepartureAtUtc,
                l.ActualArrivalAtUtc,
                l.Status))],
            Items: [.. shipment.Items.Select(i => new ShipmentItemResponse(
                i.Id,
                i.LineReference,
                i.Sku,
                i.Description,
                i.ExpectedQuantity,
                i.QuantityUnit,
                i.UnitValue,
                i.Currency))],
            CreatedAtUtc: shipment.CreatedAtUtc,
            UpdatedAtUtc: shipment.UpdatedAtUtc,
            ConcurrencyStamp: shipment.ConcurrencyStamp);

        return response;
    }
}
