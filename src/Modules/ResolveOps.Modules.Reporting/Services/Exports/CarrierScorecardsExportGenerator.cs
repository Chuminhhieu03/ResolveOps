using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Reporting;
using ResolveOps.Domain.Reporting;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Services.Exports;

/// <summary>
/// Generator for CarrierScorecards CSV export.
/// </summary>
public sealed class CarrierScorecardsExportGenerator : IExportDataGenerator
{
    public string ExportType => ResolveOps.Domain.Reporting.ExportType.CarrierScorecards;

    public async Task<(int RowCount, Stream Stream)> GenerateAsync(
        AppDbContext dbContext,
        ExportRequest request,
        CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

        var rowCount = 0;

        Guid? filterCarrierId = null;
        DateOnly? filterFromDate = null;
        DateOnly? filterToDate = null;

        if (!string.IsNullOrWhiteSpace(request.FilterCriteriaJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.FilterCriteriaJson);
                if (doc.RootElement.TryGetProperty("CarrierId", out var cProp) && cProp.TryGetGuid(out var cGuid))
                {
                    filterCarrierId = cGuid;
                }
                if (doc.RootElement.TryGetProperty("FromDate", out var fProp) && fProp.TryGetDateTime(out var fDate))
                {
                    filterFromDate = DateOnly.FromDateTime(fDate);
                }
                if (doc.RootElement.TryGetProperty("ToDate", out var tProp) && tProp.TryGetDateTime(out var tDate))
                {
                    filterToDate = DateOnly.FromDateTime(tDate);
                }
            }
            catch
            {
                // Fallback to defaults
            }
        }

        var header = new[]
        {
            "Carrier Name",
            "Total Shipments",
            "On Time Shipments",
            "Delayed Shipments",
            "On Time Rate (%)",
            "Total Exceptions",
            "Exception Rate (%)",
            "Critical Exceptions",
            "High Exceptions",
            "Total Claims",
            "Approved Claims",
            "Claim Approval Rate (%)",
            "Total Claimed Amount",
            "Total Approved Amount",
            "Total Recovered Amount",
            "Recovery Rate (%)",
            "Avg Response Time (Hours)"
        };
        await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(header));

        var query = dbContext.CarrierPerformanceSnapshots
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == request.TenantId);

        if (filterCarrierId.HasValue && filterCarrierId.Value != Guid.Empty)
        {
            query = query.Where(s => s.CarrierId == filterCarrierId.Value);
        }

        if (filterFromDate.HasValue)
        {
            query = query.Where(s => s.PeriodDate >= filterFromDate.Value);
        }

        if (filterToDate.HasValue)
        {
            query = query.Where(s => s.PeriodDate <= filterToDate.Value);
        }

        var snapshots = await query.ToListAsync(ct);

        // Group by carrier
        var grouped = snapshots
            .GroupBy(s => new { s.CarrierId, s.CarrierName })
            .Select(g =>
            {
                var totalShipments = g.Sum(x => x.TotalShipments);
                var onTimeShipments = g.Sum(x => x.OnTimeShipments);
                var delayedShipments = g.Sum(x => x.DelayedShipments);
                var exceptionCount = g.Sum(x => x.ExceptionCount);
                var criticalCount = g.Sum(x => x.CriticalSeverityCount);
                var highCount = g.Sum(x => x.HighSeverityCount);
                var totalClaims = g.Sum(x => x.TotalClaims);
                var approvedClaims = g.Sum(x => x.ApprovedClaims);
                var claimedAmount = g.Sum(x => x.TotalClaimedAmount);
                var approvedAmount = g.Sum(x => x.TotalApprovedAmount);
                var recoveredAmount = g.Sum(x => x.TotalRecoveredAmount);
                var avgResponse = g.Average(x => x.AvgResponseTimeHours);

                var onTimeRate = totalShipments > 0 ? Math.Round((double)onTimeShipments / totalShipments * 100.0, 2) : 0.0;
                var exceptionRate = totalShipments > 0 ? Math.Round((double)exceptionCount / totalShipments * 100.0, 2) : 0.0;
                var approvalRate = totalClaims > 0 ? Math.Round((double)approvedClaims / totalClaims * 100.0, 2) : 0.0;
                var recoveryRate = approvedAmount > 0 ? Math.Round((double)(recoveredAmount / approvedAmount) * 100.0, 2) : 0.0;

                return new
                {
                    g.Key.CarrierName,
                    TotalShipments = totalShipments,
                    OnTimeShipments = onTimeShipments,
                    DelayedShipments = delayedShipments,
                    OnTimeRate = onTimeRate,
                    ExceptionCount = exceptionCount,
                    ExceptionRate = exceptionRate,
                    CriticalCount = criticalCount,
                    HighCount = highCount,
                    TotalClaims = totalClaims,
                    ApprovedClaims = approvedClaims,
                    ApprovalRate = approvalRate,
                    ClaimedAmount = claimedAmount,
                    ApprovedAmount = approvedAmount,
                    RecoveredAmount = recoveredAmount,
                    RecoveryRate = recoveryRate,
                    AvgResponse = Math.Round(avgResponse, 2)
                };
            })
            .OrderBy(c => c.CarrierName)
            .ToList();

        foreach (var item in grouped)
        {
            var row = new[]
            {
                item.CarrierName,
                item.TotalShipments.ToString(CultureInfo.InvariantCulture),
                item.OnTimeShipments.ToString(CultureInfo.InvariantCulture),
                item.DelayedShipments.ToString(CultureInfo.InvariantCulture),
                item.OnTimeRate.ToString("F2", CultureInfo.InvariantCulture),
                item.ExceptionCount.ToString(CultureInfo.InvariantCulture),
                item.ExceptionRate.ToString("F2", CultureInfo.InvariantCulture),
                item.CriticalCount.ToString(CultureInfo.InvariantCulture),
                item.HighCount.ToString(CultureInfo.InvariantCulture),
                item.TotalClaims.ToString(CultureInfo.InvariantCulture),
                item.ApprovedClaims.ToString(CultureInfo.InvariantCulture),
                item.ApprovalRate.ToString("F2", CultureInfo.InvariantCulture),
                item.ClaimedAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.ApprovedAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.RecoveredAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.RecoveryRate.ToString("F2", CultureInfo.InvariantCulture),
                item.AvgResponse.ToString("F2", CultureInfo.InvariantCulture)
            };

            await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(row));
            rowCount++;
        }

        await writer.FlushAsync(ct);
        memoryStream.Position = 0;

        return (rowCount, memoryStream);
    }
}
