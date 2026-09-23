using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetExceptionAgeingReport;

public sealed class GetExceptionAgeingReportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetExceptionAgeingReportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetExceptionAgeingReportResponse>> HandleAsync(
        GetExceptionAgeingReportQuery query,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var nowUtc = _timeProvider.GetUtcNow();

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = """
            SELECT
                leg.carrier_id AS CarrierId,
                COALESCE(cr.name, 'Unassigned') AS CarrierName,
                c.exception_type AS ExceptionType,
                COUNT(CASE WHEN DATEDIFF(HOUR, c.created_at_utc, @NowUtc) < 24 THEN 1 END) AS Bucket0To24h,
                COUNT(CASE WHEN DATEDIFF(HOUR, c.created_at_utc, @NowUtc) >= 24 AND DATEDIFF(HOUR, c.created_at_utc, @NowUtc) < 48 THEN 1 END) AS Bucket24To48h,
                COUNT(CASE WHEN DATEDIFF(HOUR, c.created_at_utc, @NowUtc) >= 48 AND DATEDIFF(HOUR, c.created_at_utc, @NowUtc) < 72 THEN 1 END) AS Bucket48To72h,
                COUNT(CASE WHEN DATEDIFF(HOUR, c.created_at_utc, @NowUtc) >= 72 AND DATEDIFF(HOUR, c.created_at_utc, @NowUtc) < 168 THEN 1 END) AS Bucket3To7d,
                COUNT(CASE WHEN DATEDIFF(HOUR, c.created_at_utc, @NowUtc) >= 168 THEN 1 END) AS BucketGreaterThan7d,
                COUNT(*) AS TotalOpenCases
            FROM exception_cases c
            LEFT JOIN shipments s ON s.id = c.shipment_id AND s.tenant_id = c.tenant_id
            LEFT JOIN shipment_legs leg ON leg.shipment_id = s.id AND leg.tenant_id = s.tenant_id AND leg.sequence_number = 1
            LEFT JOIN carriers cr ON cr.id = leg.carrier_id AND cr.tenant_id = c.tenant_id
            WHERE c.tenant_id = @TenantId
              AND c.status NOT IN ('Closed', 'Cancelled')
              AND (@CarrierId IS NULL OR leg.carrier_id = @CarrierId)
              AND (@Severity IS NULL OR c.severity = @Severity)
              AND (@FromDate IS NULL OR c.created_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR c.created_at_utc <= @ToDate)
            GROUP BY leg.carrier_id, cr.name, c.exception_type
            ORDER BY TotalOpenCases DESC;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                query.TenantId,
                query.CarrierId,
                query.Severity,
                query.FromDate,
                query.ToDate,
                NowUtc = nowUtc
            },
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<ExceptionAgeingBucketDto>(command)).ToList();

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "ExceptionAgeingReport"));

        var totalOpenCases = rows.Sum(r => r.TotalOpenCases);

        var response = new GetExceptionAgeingReportResponse(
            Items: rows,
            TotalOpenCases: totalOpenCases,
            GeneratedAtUtc: nowUtc);

        return Result<GetExceptionAgeingReportResponse>.Success(response);
    }
}
