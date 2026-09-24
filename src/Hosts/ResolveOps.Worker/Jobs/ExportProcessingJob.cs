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

        var generators = scope.ServiceProvider.GetServices<ResolveOps.Modules.Reporting.Services.Exports.IExportDataGenerator>();

        foreach (var exportRequest in pendingRequests)
        {
            if (ct.IsCancellationRequested) break;

            var sw = Stopwatch.StartNew();

            exportRequest.MarkProcessing(_timeProvider);
            await dbContext.SaveChangesAsync(ct);

            try
            {
                var generator = generators.FirstOrDefault(g =>
                    string.Equals(g.ExportType, exportRequest.ExportType, StringComparison.OrdinalIgnoreCase));

                if (generator == null)
                {
                    throw new InvalidOperationException($"No export generator registered for export type '{exportRequest.ExportType}'.");
                }

                var (rowCount, stream) = await generator.GenerateAsync(dbContext, exportRequest, ct);

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

                // Create in-app notification so user can see/access download even after F5 or page navigation
                var notificationResult = ResolveOps.Domain.Notifications.Notification.Create(
                    tenantId: exportRequest.TenantId,
                    userId: exportRequest.UserId,
                    notificationClass: ResolveOps.Domain.Notifications.NotificationClass.ExportCompleted,
                    channel: ResolveOps.Domain.Notifications.NotificationChannel.InApp,
                    title: $"Export Ready: {exportRequest.ExportType}",
                    message: $"Your export containing {rowCount} rows ({fileSizeBytes:N0} bytes) is ready for download.",
                    dataJson: JsonSerializer.Serialize(new { exportId = exportRequest.Id, exportType = exportRequest.ExportType, rowCount, fileSizeBytes, actionUrl = $"/api/exports/{exportRequest.Id}" }),
                    timeProvider: _timeProvider);

                if (notificationResult.IsSuccess)
                {
                    dbContext.Notifications.Add(notificationResult.Value);
                }

                await dbContext.SaveChangesAsync(ct);

                // Push real-time notification to user via SignalR if available
                var realtimeService = scope.ServiceProvider.GetService<ResolveOps.Application.Notifications.INotificationRealtimeService>();
                if (realtimeService != null)
                {
                    _ = realtimeService.SendNotificationToUserAsync(exportRequest.TenantId, exportRequest.UserId, new
                    {
                        ExportId = exportRequest.Id,
                        ExportType = exportRequest.ExportType,
                        Status = ExportStatus.Completed,
                        ActionUrl = $"/api/exports/{exportRequest.Id}",
                        Title = $"Export Ready: {exportRequest.ExportType}",
                        Message = $"Your {exportRequest.ExportType} export ({rowCount} rows) is ready.",
                        CompletedAtUtc = exportRequest.CompletedAtUtc
                    }, CancellationToken.None);
                }

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
}
