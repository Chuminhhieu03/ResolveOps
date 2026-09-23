using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ResolveOps.Application.Documents;
using ResolveOps.Application.Reporting;
using ResolveOps.Domain.Reporting;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed class ExportProcessingJob : IJob
{
    private static readonly Action<ILogger, Guid, int, long, long, Exception?> _logExportCompleted =
        LoggerMessage.Define<Guid, int, long, long>(LogLevel.Information, new EventId(1, "ExportCompleted"),
            "Export {ExportId} completed with {RowCount} rows ({FileSizeBytes} bytes) in {ElapsedMs}ms");

    private static readonly Action<ILogger, Guid, Exception?> _logExportFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(2, "ExportFailed"),
            "Export {ExportId} failed during processing");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExportProcessingJob> _logger;

    public ExportProcessingJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<ExportProcessingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

        // 1. Fetch pending export requests
        var pendingRequests = await dbContext.ExportRequests
            .IgnoreQueryFilters()
            .Where(e => e.Status == ExportStatus.Pending)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(10)
            .ToListAsync(ct);

        if (pendingRequests.Count == 0)
        {
            return;
        }

        foreach (var exportRequest in pendingRequests)
        {
            if (ct.IsCancellationRequested) break;

            var sw = Stopwatch.StartNew();

            exportRequest.MarkProcessing(_timeProvider);
            await dbContext.SaveChangesAsync(ct);

            try
            {
                var (rowCount, stream) = await GenerateExportContentAsync(dbContext, exportRequest, ct);

                var fileSizeBytes = stream.Length;
                stream.Position = 0;

                var container = "exports";
                var blobPath = $"tenants/{exportRequest.TenantId}/exports/{exportRequest.Id:N}.csv";

                // Spec Rule 10: External object storage uploads executed outside database transactions
                await objectStorage.UploadObjectAsync(
                    container: container,
                    objectName: blobPath,
                    content: stream,
                    contentType: "text/csv; charset=utf-8",
                    ct: ct);

                exportRequest.MarkCompleted(container, blobPath, rowCount, fileSizeBytes, _timeProvider);
                await dbContext.SaveChangesAsync(ct);

                sw.Stop();
                ReportingMetrics.ExportsDurationSeconds.Record(
                    sw.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("export_type", exportRequest.ExportType));

                ReportingMetrics.ExportsTotal.Add(1,
                    new KeyValuePair<string, object?>("export_type", exportRequest.ExportType),
                    new KeyValuePair<string, object?>("status", ExportStatus.Completed));

                _logExportCompleted(_logger, exportRequest.Id, rowCount, fileSizeBytes, sw.ElapsedMilliseconds, null);
            }
            catch (Exception ex)
            {
                _logExportFailed(_logger, exportRequest.Id, ex);

                exportRequest.MarkFailed(ex.Message, _timeProvider);
                await dbContext.SaveChangesAsync(CancellationToken.None);

                ReportingMetrics.ExportsTotal.Add(1,
                    new KeyValuePair<string, object?>("export_type", exportRequest.ExportType),
                    new KeyValuePair<string, object?>("status", ExportStatus.Failed));
            }
        }
    }

    private static async Task<(int rowCount, Stream stream)> GenerateExportContentAsync(
        AppDbContext dbContext,
        ExportRequest request,
        CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

        var rowCount = 0;

        // Parse filter criteria if present
        Guid? filterCarrierId = null;
        string? filterSeverity = null;
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
