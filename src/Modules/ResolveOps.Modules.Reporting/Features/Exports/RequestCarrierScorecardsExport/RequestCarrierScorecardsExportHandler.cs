using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain;
using ResolveOps.Domain.Reporting;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestCarrierScorecardsExport;

public sealed class RequestCarrierScorecardsExportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RequestCarrierScorecardsExportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RequestCarrierScorecardsExportResponse>> HandleAsync(
        RequestCarrierScorecardsExportCommand command,
        CancellationToken cancellationToken)
    {
        var filterObj = new
        {
            command.CarrierId,
            command.FromDate,
            command.ToDate
        };

        var filterJson = JsonSerializer.Serialize(filterObj);

        var createResult = ExportRequest.Create(
            tenantId: command.TenantId,
            userId: command.UserId,
            exportType: ExportType.CarrierScorecards,
            filterCriteriaJson: filterJson,
            timeProvider: _timeProvider);

        if (createResult.IsFailure)
        {
            return Result<RequestCarrierScorecardsExportResponse>.Failure(createResult.Error);
        }

        var exportRequest = createResult.Value;
        _dbContext.ExportRequests.Add(exportRequest);

        ReportingMetrics.ExportsTotal.Add(1,
            new KeyValuePair<string, object?>("export_type", ExportType.CarrierScorecards),
            new KeyValuePair<string, object?>("status", ExportStatus.Pending));

        await _dbContext.SaveChangesAsync(cancellationToken);

        var pollUri = $"/api/exports/{exportRequest.Id}";

        return Result<RequestCarrierScorecardsExportResponse>.Success(
            new RequestCarrierScorecardsExportResponse(
                ExportId: exportRequest.Id,
                ExportType: exportRequest.ExportType,
                Status: exportRequest.Status,
                PollUri: pollUri));
    }
}
