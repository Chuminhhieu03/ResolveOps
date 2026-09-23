using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Documents;
using ResolveOps.Domain;
using ResolveOps.Domain.Reporting;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Exports.GetExportStatus;

public sealed class GetExportStatusHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IObjectStorageService _objectStorageService;

    public GetExportStatusHandler(
        AppDbContext dbContext,
        IObjectStorageService objectStorageService)
    {
        _dbContext = dbContext;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<GetExportStatusResponse>> HandleAsync(
        GetExportStatusQuery query,
        CancellationToken cancellationToken)
    {
        var exportRequest = await _dbContext.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.ExportId, cancellationToken);

        if (exportRequest == null)
        {
            return Result<GetExportStatusResponse>.Failure(
                new DomainError("ERR_EXPORT_NOT_FOUND", $"Export request '{query.ExportId}' was not found."));
        }

        // IDOR defense: only the requesting user (or tenant owner) can access the export
        if (exportRequest.UserId != query.UserId)
        {
            return Result<GetExportStatusResponse>.Failure(
                new DomainError("FORBIDDEN", "You are not authorized to access this export request."));
        }

        string? downloadUrl = null;

        if (exportRequest.Status == ExportStatus.Completed &&
            !string.IsNullOrWhiteSpace(exportRequest.Container) &&
            !string.IsNullOrWhiteSpace(exportRequest.BlobPath))
        {
            downloadUrl = await _objectStorageService.GenerateDownloadPresignedUrlAsync(
                container: exportRequest.Container,
                objectName: exportRequest.BlobPath,
                expiry: TimeSpan.FromMinutes(30),
                downloadFileName: $"export-{exportRequest.Id:N}.csv",
                ct: cancellationToken);
        }

        var response = new GetExportStatusResponse(
            ExportId: exportRequest.Id,
            ExportType: exportRequest.ExportType,
            Status: exportRequest.Status,
            RowCount: exportRequest.RowCount,
            FileSizeBytes: exportRequest.FileSizeBytes,
            DownloadUrl: downloadUrl,
            ErrorMessage: exportRequest.ErrorMessage,
            CompletedAtUtc: exportRequest.CompletedAtUtc,
            CreatedAtUtc: exportRequest.CreatedAtUtc);

        return Result<GetExportStatusResponse>.Success(response);
    }
}
