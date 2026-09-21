using System;

using ResolveOps.Domain.Claims.ValueObjects;

namespace ResolveOps.Domain.Claims;

public sealed class ClaimLossComponent
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public string ComponentType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public decimal? UnitAmount { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Guid? SourceDocumentId { get; private set; }

    private ClaimLossComponent() { } // EF Core

    internal ClaimLossComponent(
        Guid id,
        Guid claimId,
        string componentType,
        string description,
        decimal? quantity,
        decimal? unitAmount,
        Money amount,
        Guid? sourceDocumentId)
    {
        if (!LossComponentType.All.Contains(componentType))
            throw new ArgumentException("Invalid component type", nameof(componentType));

        Id = id;
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
