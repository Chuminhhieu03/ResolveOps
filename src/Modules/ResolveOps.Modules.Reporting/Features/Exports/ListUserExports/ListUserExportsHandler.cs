using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Documents;
using ResolveOps.Domain;
using ResolveOps.Domain.Reporting;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Exports.ListUserExports;

public sealed class ListUserExportsHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IObjectStorageService _objectStorageService;

    public ListUserExportsHandler(AppDbContext dbContext, IObjectStorageService objectStorageService)
    {
        _dbContext = dbContext;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<ListUserExportsResponse>> HandleAsync(
        ListUserExportsQuery query,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.ExportRequests
            .Where(e => e.UserId == query.UserId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var requests = await baseQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<ExportItemDto>(requests.Count);

        foreach (var req in requests)
        {
            string? downloadUrl = null;
            if (req.Status == ExportStatus.Completed &&
                !string.IsNullOrWhiteSpace(req.Container) &&
                !string.IsNullOrWhiteSpace(req.BlobPath))
            {
                try
                {
                    downloadUrl = await _objectStorageService.GenerateDownloadPresignedUrlAsync(
                        container: req.Container,
                        objectName: req.BlobPath,
                        expiry: TimeSpan.FromMinutes(30),
                        downloadFileName: $"export-{req.Id:N}.csv",
                        ct: cancellationToken);
                }
                catch
                {
                    // Presigned URL generation failure shouldn't fail the whole listing
                }
            }

            items.Add(new ExportItemDto(
                Id: req.Id,
                ExportType: req.ExportType,
                Status: req.Status,
                RowCount: req.RowCount,
                FileSizeBytes: req.FileSizeBytes,
                DownloadUrl: downloadUrl,
                ErrorMessage: req.ErrorMessage,
                CreatedAtUtc: req.CreatedAtUtc,
                CompletedAtUtc: req.CompletedAtUtc));
        }

        return Result<ListUserExportsResponse>.Success(
            new ListUserExportsResponse(
                Items: items,
                PageNumber: query.PageNumber,
                PageSize: query.PageSize,
                TotalCount: totalCount));
    }
}
