using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain;
using ResolveOps.Domain.Reporting;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestClaimsExport;

public sealed class RequestClaimsExportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RequestClaimsExportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RequestClaimsExportResponse>> HandleAsync(
        RequestClaimsExportCommand command,
        CancellationToken cancellationToken)
    {
        var filterObj = new
        {
            command.CarrierId,
            command.Status,
            command.FromDate,
            command.ToDate
        };

        var filterJson = JsonSerializer.Serialize(filterObj);

        var createResult = ExportRequest.Create(
            tenantId: command.TenantId,
            userId: command.UserId,
            exportType: ExportType.Claims,
            filterCriteriaJson: filterJson,
            timeProvider: _timeProvider);

        if (createResult.IsFailure)
        {
            return Result<RequestClaimsExportResponse>.Failure(createResult.Error);
        }

        var exportRequest = createResult.Value;
        _dbContext.ExportRequests.Add(exportRequest);

        ReportingMetrics.ExportsTotal.Add(1,
            new KeyValuePair<string, object?>("export_type", ExportType.Claims),
            new KeyValuePair<string, object?>("status", ExportStatus.Pending));

        await _dbContext.SaveChangesAsync(cancellationToken);

        var pollUri = $"/api/exports/{exportRequest.Id}";

        return Result<RequestClaimsExportResponse>.Success(
            new RequestClaimsExportResponse(
                ExportId: exportRequest.Id,
                ExportType: exportRequest.ExportType,
                Status: exportRequest.Status,
                PollUri: pollUri));
    }
}
