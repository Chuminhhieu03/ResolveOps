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
/// Generator for ExceptionCases CSV export.
/// </summary>
public sealed class ExceptionCasesExportGenerator : IExportDataGenerator
{
    public string ExportType => ResolveOps.Domain.Reporting.ExportType.ExceptionCases;

    public async Task<(int RowCount, Stream Stream)> GenerateAsync(
        AppDbContext dbContext,
        ExportRequest request,
        CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

        var rowCount = 0;

        // Parse filter criteria if present
        string? filterSeverity = null;
        DateTimeOffset? filterFromDate = null;
        DateTimeOffset? filterToDate = null;

        if (!string.IsNullOrWhiteSpace(request.FilterCriteriaJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.FilterCriteriaJson);
                if (doc.RootElement.TryGetProperty("Severity", out var sProp))
                {
                    filterSeverity = sProp.GetString();
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

        // CSV Header
        var header = new[]
        {
            "Case Number",
            "Exception Type",
            "Severity",
            "Status",
            "Detected At (UTC)",
            "Owner Team",
            "Shipment External Reference"
        };
        await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(header));

        // Stream exception cases
        var query = dbContext.ExceptionCases
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == request.TenantId);

        if (!string.IsNullOrWhiteSpace(filterSeverity))
        {
            query = query.Where(c => c.Severity == filterSeverity.Trim());
        }

        if (filterFromDate.HasValue)
        {
            query = query.Where(c => c.CreatedAtUtc >= filterFromDate.Value);
        }

        if (filterToDate.HasValue)
        {
            query = query.Where(c => c.CreatedAtUtc <= filterToDate.Value);
        }

        var cases = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(10000)
            .Select(c => new
            {
                c.CaseNumber,
                c.ExceptionType,
                c.Severity,
                c.Status,
                c.CreatedAtUtc,
                c.OwnerTeamCode,
                ShipmentRef = dbContext.Shipments
                    .Where(s => s.Id == c.ShipmentId && s.TenantId == c.TenantId)
                    .Select(s => s.ExternalReference)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(ct);

        foreach (var item in cases)
        {
            var row = new[]
            {
                item.CaseNumber,
                item.ExceptionType,
                item.Severity,
                item.Status,
                item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                item.OwnerTeamCode ?? string.Empty,
                item.ShipmentRef
            };

            await writer.WriteLineAsync(CsvFormulaEscaper.FormatRow(row));
            rowCount++;
        }

        await writer.FlushAsync(ct);
        memoryStream.Position = 0;

        return (rowCount, memoryStream);
    }
}
