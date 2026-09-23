using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain;
using ResolveOps.Domain.Reporting;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestExceptionCasesExport;

public sealed class RequestExceptionCasesExportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RequestExceptionCasesExportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RequestExceptionCasesExportResponse>> HandleAsync(
        RequestExceptionCasesExportCommand command,
        CancellationToken cancellationToken)
    {
        var filterObj = new
        {
            command.CarrierId,
            command.Severity,
            command.FromDate,
            command.ToDate
        };

        var filterJson = JsonSerializer.Serialize(filterObj);

        var createResult = ExportRequest.Create(
            tenantId: command.TenantId,
            userId: command.UserId,
            exportType: ExportType.ExceptionCases,
            filterCriteriaJson: filterJson,
            timeProvider: _timeProvider);

        if (createResult.IsFailure)
        {
            return Result<RequestExceptionCasesExportResponse>.Failure(createResult.Error);
        }

        var exportRequest = createResult.Value;
        _dbContext.ExportRequests.Add(exportRequest);

        ReportingMetrics.ExportsTotal.Add(1,
            new KeyValuePair<string, object?>("export_type", ExportType.ExceptionCases),
            new KeyValuePair<string, object?>("status", ExportStatus.Pending));

        await _dbContext.SaveChangesAsync(cancellationToken);

        var pollUri = $"/api/exports/{exportRequest.Id}";

        var response = new RequestExceptionCasesExportResponse(
            ExportId: exportRequest.Id,
            Status: exportRequest.Status,
            PollUri: pollUri,
            CreatedAtUtc: exportRequest.CreatedAtUtc);

        return Result<RequestExceptionCasesExportResponse>.Success(response);
    }
}
