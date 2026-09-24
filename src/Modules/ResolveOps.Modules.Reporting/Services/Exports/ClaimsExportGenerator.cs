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
/// Generator for Claims CSV export.
/// </summary>
public sealed class ClaimsExportGenerator : IExportDataGenerator
{
    public string ExportType => ResolveOps.Domain.Reporting.ExportType.Claims;

    public async Task<(int RowCount, Stream Stream)> GenerateAsync(
        AppDbContext dbContext,
        ExportRequest request,
        CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

        var rowCount = 0;

        Guid? filterCarrierId = null;
        string? filterStatus = null;
        DateTimeOffset? filterFromDate = null;
        DateTimeOffset? filterToDate = null;

        if (!string.IsNullOrWhiteSpace(request.FilterCriteriaJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.FilterCriteriaJson);
                if (doc.RootElement.TryGetProperty("CarrierId", out var cProp) && cProp.TryGetGuid(out var cGuid))
                {
                    filterCarrierId = cGuid;
                }
                if (doc.RootElement.TryGetProperty("Status", out var sProp))
                {
                    filterStatus = sProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("FromDate", out var fProp) && fProp.TryGetDateTimeOffset(out var fDate))
                {
                    filterFromDate = fDate;
                }
                if (doc.RootElement.TryGetProperty("ToDate", out var tProp) && tProp.TryGetDateTimeOffset(out var tDate))
                {
                    filterToDate = tDate;
                }
            }
            catch
            {
                // Fallback to no filters if JSON malformed
            }
        }

        var header = new[]
        {
            "Claim Number",
            "Case Number",
            "Carrier Name",
            "Claim Type",
            "Status",
            "Eligibility Status",
            "Claimed Amount",
            "Approved Amount",
            "Recovered Amount",
            "Currency",
            "Created At (UTC)",
            "Submitted At (UTC)",
            "Closed At (UTC)"
        };
        await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(header));

        var query = dbContext.Claims
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == request.TenantId);

        if (filterCarrierId.HasValue && filterCarrierId.Value != Guid.Empty)
        {
            query = query.Where(c => c.CarrierId == filterCarrierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filterStatus))
        {
            query = query.Where(c => c.Status == filterStatus.Trim());
        }

        if (filterFromDate.HasValue)
        {
            query = query.Where(c => c.CreatedAtUtc >= filterFromDate.Value);
        }

        if (filterToDate.HasValue)
        {
            query = query.Where(c => c.CreatedAtUtc <= filterToDate.Value);
        }

        var claims = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(10000)
            .Select(c => new
            {
                c.ClaimNumber,
                CaseNumber = dbContext.ExceptionCases
                    .Where(ec => ec.Id == c.CaseId && ec.TenantId == c.TenantId)
                    .Select(ec => ec.CaseNumber)
                    .FirstOrDefault() ?? string.Empty,
                CarrierName = dbContext.Carriers
                    .Where(ca => ca.Id == c.CarrierId && ca.TenantId == c.TenantId)
                    .Select(ca => ca.Name)
                    .FirstOrDefault() ?? "Unknown",
                c.ClaimType,
                c.Status,
                c.EligibilityStatus,
                c.ClaimedAmount,
                c.ApprovedAmount,
                c.RecoveredAmount,
                c.Currency,
                c.CreatedAtUtc,
                c.SubmittedAtUtc,
                c.ClosedAtUtc
            })
            .ToListAsync(ct);

        foreach (var item in claims)
        {
            var row = new[]
            {
                item.ClaimNumber,
                item.CaseNumber,
                item.CarrierName,
                item.ClaimType,
                item.Status,
                item.EligibilityStatus,
                item.ClaimedAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.ApprovedAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.RecoveredAmount.ToString("F2", CultureInfo.InvariantCulture),
                item.Currency,
                item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                item.SubmittedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
                item.ClosedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty
            };

            await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(row));
            rowCount++;
        }

        await writer.FlushAsync(ct);
        memoryStream.Position = 0;

        return (rowCount, memoryStream);
    }
}
