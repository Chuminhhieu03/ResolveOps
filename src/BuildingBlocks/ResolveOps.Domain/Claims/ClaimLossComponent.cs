using System;
using ResolveOps.Domain.Claims.Enums;
using ResolveOps.Domain.Claims.ValueObjects;

namespace ResolveOps.Domain.Claims;

public class ClaimLossComponent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClaimId { get; private set; }
    public LossComponentType ComponentType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public decimal? UnitAmount { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Guid? SourceDocumentId { get; private set; }

    private ClaimLossComponent() { } // EF Core

    internal ClaimLossComponent(
        Guid id,
        Guid tenantId,
        Guid claimId,
        LossComponentType componentType,
        string description,
        decimal? quantity,
        decimal? unitAmount,
        Money amount,
        Guid? sourceDocumentId)
    {
        Id = id;
        TenantId = tenantId;
        ClaimId = claimId;
        ComponentType = componentType;
        Description = description;
        Quantity = quantity;
        UnitAmount = unitAmount;
        Amount = amount;
        SourceDocumentId = sourceDocumentId;
    }

    internal void Update(
        string description,
        decimal? quantity,
        decimal? unitAmount,
        Money total,
        Guid? sourceDocumentId)
    {
        Description = description;
        Quantity = quantity;
        UnitAmount = unitAmount;
        Amount = total;
        SourceDocumentId = sourceDocumentId;
    }
}
