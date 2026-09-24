using System;

namespace ResolveOps.Domain.Reporting;

/// <summary>
/// Aggregated daily performance read model for a carrier within a tenant.
/// Acts as a high-performance daily rollup bucket (OLAP read model) to prevent expensive
/// full-table scans across millions of transactional records when rendering scorecards.
/// </summary>
public sealed class CarrierPerformanceSnapshot : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CarrierId { get; private set; }
    public string CarrierName { get; private set; } = string.Empty;

    /// <summary>
    /// The daily rollup activity date (UTC calendar date) for this snapshot.
    /// Events occurring on this calendar day increment their respective metric counters.
    /// When querying carrier scorecards over a date range [FromDate, ToDate], snapshots
    /// within that window are summed, delivering sub-second query performance.
    /// </summary>
    public DateOnly PeriodDate { get; private set; }
    public int TotalShipments { get; private set; }
    public int OnTimeShipments { get; private set; }
    public int DelayedShipments { get; private set; }
    public int ExceptionCount { get; private set; }
    public int CriticalSeverityCount { get; private set; }
    public int HighSeverityCount { get; private set; }
    public int MediumSeverityCount { get; private set; }
    public int LowSeverityCount { get; private set; }
    public int TotalClaims { get; private set; }
    public int ApprovedClaims { get; private set; }
    public int RejectedClaims { get; private set; }
    public decimal TotalClaimedAmount { get; private set; }
    public decimal TotalApprovedAmount { get; private set; }
    public decimal TotalRecoveredAmount { get; private set; }
    public double AvgResponseTimeHours { get; private set; }
    public DateTimeOffset LastCalculatedAtUtc { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private CarrierPerformanceSnapshot() { } // EF Core

    public static CarrierPerformanceSnapshot Create(
        Guid tenantId,
        Guid carrierId,
        string carrierName,
        DateOnly periodDate,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new CarrierPerformanceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CarrierId = carrierId,
            CarrierName = carrierName.Trim(),
            PeriodDate = periodDate,
            TotalShipments = 0,
            OnTimeShipments = 0,
            DelayedShipments = 0,
            ExceptionCount = 0,
            CriticalSeverityCount = 0,
            HighSeverityCount = 0,
            MediumSeverityCount = 0,
            LowSeverityCount = 0,
            TotalClaims = 0,
            ApprovedClaims = 0,
            RejectedClaims = 0,
            TotalClaimedAmount = 0m,
            TotalApprovedAmount = 0m,
            TotalRecoveredAmount = 0m,
            AvgResponseTimeHours = 0.0,
            LastCalculatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void RecordShipment(TimeProvider timeProvider)
    {
        TotalShipments++;
        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }

    public void RecordTrackingEvent(bool isDelayed, bool isOnTime, TimeProvider timeProvider)
    {
        if (isDelayed)
        {
            DelayedShipments++;
        }

        if (isOnTime)
        {
            OnTimeShipments++;
        }

        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }

    public void RecordException(string severity, TimeProvider timeProvider)
    {
        ExceptionCount++;
        switch (severity.Trim().ToLowerInvariant())
        {
            case "critical":
                CriticalSeverityCount++;
                break;
            case "high":
                HighSeverityCount++;
                break;
            case "medium":
                MediumSeverityCount++;
                break;
            case "low":
                LowSeverityCount++;
                break;
            default:
                MediumSeverityCount++;
                break;
        }

        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }

    public void RecordClaimSubmitted(decimal claimedAmount, TimeProvider timeProvider)
    {
        TotalClaims++;
        TotalClaimedAmount += claimedAmount;
        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }

    public void RecordClaimDecision(string decision, decimal approvedAmount, double? responseTimeHours, TimeProvider timeProvider)
    {
        var lower = decision.Trim().ToLowerInvariant();
        if (lower.Contains("approved"))
        {
            ApprovedClaims++;
            TotalApprovedAmount += approvedAmount;
        }
        else if (lower.Contains("denied") || lower.Contains("rejected"))
        {
            RejectedClaims++;
        }

        if (responseTimeHours.HasValue && responseTimeHours.Value >= 0)
        {
            var decidedCount = ApprovedClaims + RejectedClaims;
            if (decidedCount <= 1)
            {
                AvgResponseTimeHours = responseTimeHours.Value;
            }
            else
            {
                // Rolling average
                AvgResponseTimeHours = ((AvgResponseTimeHours * (decidedCount - 1)) + responseTimeHours.Value) / decidedCount;
            }
        }

        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }

    public void RecordClaimRecovery(decimal recoveredAmount, TimeProvider timeProvider)
    {
        TotalRecoveredAmount += recoveredAmount;
        LastCalculatedAtUtc = timeProvider.GetUtcNow();
        UpdatedAtUtc = LastCalculatedAtUtc;
    }
}
