using System;

namespace ResolveOps.Domain.Claims;

public sealed class CarrierClaimResponse
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string ResponseType { get; private set; } = string.Empty;
    public string? CarrierReference { get; private set; }
    public DateTimeOffset ResponseAtUtc { get; private set; }
    public decimal? ApprovedAmount { get; private set; }
    public string? Currency { get; private set; }
    public string[] ReasonCodes { get; private set; } = [];
    public string? Notes { get; private set; }
    public Guid RecordedBy { get; private set; }
    public string SourceChannel { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private CarrierClaimResponse() { } // EF Core

    public static CarrierClaimResponse Create(
        Guid tenantId,
        Guid claimId,
        string responseType,
        string? carrierReference,
        DateTimeOffset responseAtUtc,
        decimal? approvedAmount,
        string? currency,
        string[] reasonCodes,
        string? notes,
        Guid recordedBy,
        string sourceChannel,
        TimeProvider timeProvider)
    {
        if (!CarrierResponseType.All.Contains(responseType))
            throw new ArgumentException($"Invalid carrier response type: {responseType}", nameof(responseType));

        if (!Claims.SourceChannel.All.Contains(sourceChannel))
            throw new ArgumentException($"Invalid source channel: {sourceChannel}", nameof(sourceChannel));

        return new CarrierClaimResponse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClaimId = claimId,
            ResponseType = responseType,
            CarrierReference = carrierReference,
            ResponseAtUtc = responseAtUtc,
            ApprovedAmount = approvedAmount,
            Currency = currency,
            ReasonCodes = reasonCodes ?? [],
            Notes = notes,
            RecordedBy = recordedBy,
            SourceChannel = sourceChannel,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };
    }
}
